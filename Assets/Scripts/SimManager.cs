using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using static OpenMetaverse.Primitive;

#if useHDRP
using UnityEngine.Rendering.HighDefinition;
#endif

using CrystalFrost;
using static CrystalFrost.CFAssetManager;
using CrystalFrost.Assets;
using CrystalFrost.Assets.Textures;
using CrystalFrost.Assets.Mesh;
using CrystalFrost.Config;
using CrystalFrost.Extensions;
using CrystalFrost.Lib;
using CrystalFrost.Logging;
using CrystalFrost.UnityRendering;
using CrystalFrost.WorldState;
using Unity.VisualScripting;

#if USE_KWS
using KWS;
using static KWS.WaterSystem;
#endif

#if USE_FUNLY_SKY
using Funly.SkyStudio;
#endif

using static Bunny.HUDHelper;
using OMVVector3 = OpenMetaverse.Vector3;
using Vector3 = UnityEngine.Vector3;
using Material = UnityEngine.Material;
using Quaternion = UnityEngine.Quaternion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CrystalFrost.Assets.Animation;
using Temp;
using UnityEngine.Animations;
using Util;
using Debug = UnityEngine.Debug;

public class SimManager : MonoBehaviour
{
	const float DEG_TO_RAD = 0.017453292519943295769236907684886f;
	const float RAD_TO_DEG = 57.295779513082320876798154814105f;

	[SerializeField] Transform player;
	[SerializeField] GameObject cube;
	[SerializeField] GameObject blank;
	[SerializeField] Material opaqueMat;
	[SerializeField] Material opaqueFullBrightMat;
	[SerializeField] Material alphaMat;
	[SerializeField] Material alphaFullBrightMat;

	public Transform water;

	public Queue<PrimEventArgs> objectsToRez = new();

	/// <summary>
	/// All the known simulators indexed by their handle
	/// </summary>
	public readonly ConcurrentDictionary<ulong, SimulatorContainer> simulators = new();

	Avatar avatar;
	public BoundsOctree<GameObject> boundsTree = new(15, new Vector3(127, 0, 127), 1, 1.25f);
	public Transform cameraRoot;
	public MeshObjectManager meshObjectManager;

	public Transform[] hudAnchors;

	private IReadyTextureQueue _readyTextureQueue;
	private IDecodedMeshQueue _decodedMeshQueue;
	private static readonly WaitForSecondsRealtime Wait100ms = new(0.1f);

	// private IAssetManager _assetManager;
	private CodeConfig _config;
	private IStateManager _stateManager;
	private INewSimObjectQueue _newObjectQueue;
	private ISceneObjectsNeedingRenderersQueue _needRenderDataQueue;
	private IDecodedAnimationQueue _decodedAnimationQueue;

	private IUnityRenderManager _renderManager;
	private ILogger<SimManager> _log;
	private ILMVLogger _lmvLogger;
	public Transform sun;


	// Start is called before the first frame update
	GridClient client;
	public string simName;
	public string simOwner;
	public ulong thissim;
	public Simulator _thissim;
	public bool scenePrimsContainsAvatar = false;

	public ConcurrentDictionary<UUID, uint> scenePrimIndexUUID = new();
	public ConcurrentDictionary<uint, ScenePrimData> scenePrims = new();
	public Dictionary<uint, List<Primitive>> orphanedPrims = new();
	public List<MeshRequestData> meshRequests = new();

	/// <summary>
	/// Honestly not sure what this update event is even about
	/// The documentation on libOpenMetaverse and libreMetaverse
	/// is incredibly scant, but it appears to be
	/// Yet Another Prim Update Event
	/// </summary>
	public ConcurrentQueue<ObjectDataBlockUpdateEventArgs> objectDataBlockUpdates = new();

	private readonly ConcurrentQueue<KillObjectEventData> _killObjectQueue = new();
	private readonly ConcurrentQueue<Tuple<Primitive, UUID>> _loadedAnimationRequest = new();
	private readonly ConcurrentQueue<AvatarUpdateEventArgs> _avatarUpdates = new();
	private readonly ConcurrentQueue<ObjectPropertiesUpdatedEventArgs> _objectPropertiesUpdateEvents = new();
	private readonly ConcurrentQueue<ObjectPropertiesEventArgs> _objectPropertiesEvents = new();
	private readonly ConcurrentQueue<UUIDNameReplyEvent> _nameReplyEvents = new();
	private ConcurrentQueue<ScenePrimData> unTexturedPrims = new();

	void AvatarNamesEventHandler(object sender, UUIDNameReplyEventArgs e)
	{
		foreach (KeyValuePair<UUID, string> kvp in e.Names)
		{
			if (scenePrimIndexUUID.ContainsKey(kvp.Key))
			{
				if (scenePrims.TryGetValue(scenePrimIndexUUID[kvp.Key], out ScenePrimData sPrim))
				{
					//Debug.Log($"NAME RECEIVED: {kvp.Value}");
					_nameReplyEvents.Enqueue(new UUIDNameReplyEvent { uuid = kvp.Key, name = kvp.Value });
					//UnityMainThreadDispatcher.Instance().Enqueue(() => sPrim.SetName(kvp.Value));
				}
			}
			else
			{
				ClientManager.client.Avatars.RequestAvatarName(kvp.Key);
			}
		}
	}

	// Do an action on all the simulators
	// TODO: Figure out locking (should list be locked when iterating?). Maybe use yield return?
	private void ForEachSimContainer(Action<SimulatorContainer> action)
	{
		foreach (var kvp in simulators)
		{
			action(kvp.Value);
		}
	}

	private void Awake()
	{
		_log = Services.GetService<ILogger<SimManager>>();
		_lmvLogger = Services.GetService<ILMVLogger>(); // just needs to exist to function.
		_readyTextureQueue = Services.GetService<IReadyTextureQueue>();
		_decodedMeshQueue = Services.GetService<IDecodedMeshQueue>();

		// _assetManager = Services.GetService<IAssetManager>();
		_decodedAnimationQueue = Services.GetService<IDecodedAnimationQueue>();
		_config = Services.GetService<IOptions<CodeConfig>>().Value;

		if (_config.UseNewObjectGraph)
		{
			// Creating the state manager, will cause it to subscribe to the
			// update events and run the code tha builds the new object graph.
			_stateManager = Services.GetService<IStateManager>();
			_newObjectQueue = Services.GetService<INewSimObjectQueue>();
			_renderManager = Services.GetService<IUnityRenderManager>();
			_needRenderDataQueue = Services.GetService<ISceneObjectsNeedingRenderersQueue>();
		}

		ClientManager.assetManager.simManager = this;
		ClientManager.simManager = this;

		if (this.meshObjectManager == null)
		{
			Debug.LogWarning("MeshObjectManager not set in SimManager, trying to find it");
			this.meshObjectManager = FindFirstObjectByType<MeshObjectManager>();
		}
	}

	private void Start()
	{
		client = ClientManager.client;

		avatar = gameObject.GetComponent<Avatar>();
		ClientManager.assetManager.materialContainer.Add(UUID.Zero,
			new MaterialContainer(UUID.Zero, Texture2D.whiteTexture, 3));

		client.Objects.AvatarUpdate += AvatarUpdateHandler;
		client.Objects.KillObject += KillObjectEventHandler;
		client.Objects.KillObjects += KillObjectsEventHandler;
		client.Objects.ObjectDataBlockUpdate += ObjectDataBlockUpdateEvent;
		client.Objects.ObjectProperties += ObjectPropertiesEventHandler;
		client.Objects.ObjectPropertiesUpdated += ObjectPropertiesUpdateHandler;
		client.Objects.ObjectUpdate += Objects_ObjectUpdate;
		client.Objects.PhysicsProperties += PhysicsPropertiesEvent;
		client.Objects.TerseObjectUpdate += Objects_TerseObjectUpdate;

		client.Avatars.AvatarAnimation += AvatarAnimationHandler;

		client.Terrain.LandPatchReceived += TerrainEventHandler;
		client.Avatars.UUIDNameReply += AvatarNamesEventHandler;


		StartCoroutine(UpdateCamera());
		StartCoroutine(CleanUnusedAssets());
		objectProximityHandler = new ObjectProximityHandler(objectUpdates, client);
		objectProximityHandler.UpdateCameraProperties(Camera.main);
	}

	private void OnDestroy()
	{
		client.Objects.AvatarUpdate -= AvatarUpdateHandler;
		client.Objects.KillObject -= KillObjectEventHandler;
		client.Objects.KillObjects -= KillObjectsEventHandler;
		client.Objects.ObjectDataBlockUpdate -= ObjectDataBlockUpdateEvent;
		client.Objects.ObjectProperties -= ObjectPropertiesEventHandler;
		client.Objects.ObjectPropertiesUpdated -= ObjectPropertiesUpdateHandler;
		client.Objects.ObjectUpdate -= Objects_ObjectUpdate;
		client.Objects.PhysicsProperties -= PhysicsPropertiesEvent;
		client.Objects.TerseObjectUpdate -= Objects_TerseObjectUpdate;

		client.Avatars.AvatarAnimation -= AvatarAnimationHandler;
		client.Terrain.LandPatchReceived -= TerrainEventHandler;
		client.Avatars.UUIDNameReply -= AvatarNamesEventHandler;
	}

	/// <summary>
	/// A new simulator connected. Add it to our collection and initialize management of it.
	/// </summary>
	/// <param name="pSim">The OpenMetaverse class for the simulator</param>
	/// <param name="pX">Absolute world position of simulator</param>
	/// <param name="pY"Absolute world position of simulator></param>
	/// <param name="pRels">Relative world position of simulator relative to the current simulator</param>
	/// <param name="pRely">Relative world position of simulator relative to the current simulator</param>
	public void SimConnected(Simulator pSim, uint pX, uint pY, uint pRels, uint pRely)
	{
		var sim = new SimulatorContainer(pSim, pX, pY);
		simulators.TryAdd(pSim.Handle, sim);
	}


	private IEnumerator CleanUnusedAssets()
	{
		while (true)
		{
			Resources.UnloadUnusedAssets();
			yield return new WaitForSecondsRealtime(10f);
		}
	}

	private void AvatarUpdateHandler(object sender, AvatarUpdateEventArgs e)
	{
		this.objectProximityHandler.AddPrimPositionID(e.Avatar.LocalID, e.Avatar.ParentID, e.Avatar.Position);
		_avatarUpdates.Enqueue(e);
	}

	private void AvatarUpdates()
	{
		Primitive prim;
		while (_avatarUpdates.Count > 0)
		{
			_avatarUpdates.TryDequeue(out AvatarUpdateEventArgs e);
			meshObjectManager.OnAvatarUpdate(e);
			prim = e.Avatar;
			if (e.IsNew)
			{
				GameObject bgo = Instantiate<GameObject>(blank);
				bgo.name = $"avatar {prim.LocalID} heirarchy holder";

				GameObject go = Instantiate<GameObject>(cube);
				go.name = $"avatar {prim.LocalID}";


				GameObject meshHolder = Instantiate<GameObject>(blank);
				go.transform.parent = bgo.transform;
				go.transform.localPosition = Vector3.zero;
				meshHolder.transform.parent = bgo.transform;

				//Debug.Log($"Object: {go.name}");
				ScenePrimData sPD = new(go, prim);
				if (scenePrims.TryAdd(prim.LocalID, sPD))
				{
					go.transform.localScale = prim.Scale.ToUnity();
					scenePrims[prim.LocalID].uuid = prim.ID;
					scenePrims[prim.LocalID].ConstructionHash = prim.PrimData.GetHashCode();
					if (prim.ParentID != 0)
					{
						if (!scenePrims.ContainsKey(prim.ParentID))
						{
							orphanedPrims.TryAdd(prim.ParentID, new List<Primitive>());
							orphanedPrims[prim.ParentID].Add(prim);
							go.transform.position = new Vector3(5000f, 5000f, 5000f);
						}
						else
						{
							bgo.transform.parent = scenePrims[prim.ParentID].obj.transform.parent;
						}
					}

					bgo.transform.SetLocalPositionAndRotation(
						prim.Position.ToUnity(),
						prim.Rotation.ToUnity());

					if (orphanedPrims.ContainsKey(prim.ParentID))
					{
						foreach (Primitive p in orphanedPrims[prim.ParentID])
						{
							if (p.ParentID == prim.ParentID && scenePrims.ContainsKey(prim.ParentID))
							{
								scenePrims[p.LocalID].obj.transform.parent.parent =
									scenePrims[prim.ParentID].obj.transform.parent;
								bgo.transform.SetLocalPositionAndRotation(
									prim.Position.ToUnity(),
									p.Rotation.ToUnity());

								if (IsHUD(scenePrims[p.LocalID].prim))
								{
									bgo.SetLayerRecursively(8);
								}

								HandleAttachment(bgo, prim);

								CleanOrphanedPrims(prim);
							}
						}
					}

					scenePrims[prim.LocalID].Render();

					//OpenSim doesn't send avatar objects with the pcode of Avatar, so this we have to do new avatar shit here if OpenSim
					if (ClientManager.isOpenSim)
					{
						scenePrims[prim.LocalID].DoAvatarStuff();
					}
				}
				else
				{
					if (scenePrims.ContainsKey(prim.LocalID))
					{
						Debug.LogWarning(
							$"Tried to create new prim, but localID:{prim.LocalID} already exists in scene.");
					}
					else
					{
						Debug.LogWarning($"Error adding prim to dictionary.");
					}

					Destroy(bgo);
				}

				if (e.Avatar.LocalID == client.Self.LocalID)
				{
					ClientManager.currentOutfitFolder = new CurrentOutfitFolder();
					ScenePrimData spd = ClientManager.simManager.scenePrims[ClientManager.client.Self.LocalID];
					avatar.myAvatar.parent = spd.meshHolder.transform.root;
					avatar.myAvatar.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
					avatar.rotation = spd.prim.Rotation.ToUnity();
				}
			}
			else
			{
				if (scenePrims.ContainsKey(e.Avatar.LocalID))
				{
					scenePrimsContainsAvatar = true;
					scenePrims[e.Avatar.LocalID].prim = e.Avatar;
					scenePrims[e.Avatar.LocalID].velocity = e.Avatar.Velocity.ToVector3();
					scenePrims[e.Avatar.LocalID].omega = e.Avatar.AngularVelocity.ToVector3();
				}
			}
		}
	}

	private void HandleAttachment(GameObject bgo, Primitive prim)
	{
		if (prim.IsAttachment)
		{
			Transform _av = bgo.transform.root.Find("Avatar");
			if (_av != null)
			{
				Transform _attachpoint = FindChildByNameRecursive(_av, prim.PrimData.AttachmentPoint.ToString());
				if (_attachpoint != null)
				{
					AddPositionAndRotationConstraints(bgo, _attachpoint, prim.Position.ToVector3(),
						prim.Rotation.ToUnity().eulerAngles);
				}
			}
		}
	}

	private void ObjectPropertiesUpdateHandler(object sender, ObjectPropertiesUpdatedEventArgs e)
	{
		this.objectProximityHandler.AddPrimPositionID(e.Prim);
		_objectPropertiesUpdateEvents.Enqueue(e);
	}

	private void PhysicsPropertiesEvent(object sender, PhysicsPropertiesEventArgs e)
	{
		return;
	}

	private void NameReplyEvents()
	{
		Queue<UUIDNameReplyEvent> newQueue = new();
		while (_nameReplyEvents.Count > 0)
		{
			if (_nameReplyEvents.TryDequeue(out UUIDNameReplyEvent nameReplyEvent))
			{
				if (scenePrimIndexUUID.ContainsKey(nameReplyEvent.uuid))
				{
					if (scenePrims.ContainsKey(scenePrimIndexUUID[nameReplyEvent.uuid]))
					{
						scenePrims[scenePrimIndexUUID[nameReplyEvent.uuid]].SetName(nameReplyEvent.name);
						ClientManager.chat.SetKeyToName(nameReplyEvent.uuid, nameReplyEvent.name);
						ClientManager.chatWindow.contactsList.UpdateContact(nameReplyEvent.name, nameReplyEvent.uuid);
					}
					else
					{
						newQueue.Enqueue(nameReplyEvent);
					}
				}
				else
				{
					newQueue.Enqueue(nameReplyEvent);
				}
			}
		}

		while (newQueue.Count > 0)
		{
			_nameReplyEvents.Enqueue(newQueue.Dequeue());
		}
	}


	private void ObjectPropertyEvents()
	{
		while (_objectPropertiesEvents.Count > 0)
		{
			if (_objectPropertiesEvents.TryDequeue(out ObjectPropertiesEventArgs objectPropertiesEvent))
			{
				if (scenePrims.ContainsKey(scenePrimIndexUUID[objectPropertiesEvent.Properties.ObjectID]))
				{
					scenePrims[scenePrimIndexUUID[objectPropertiesEvent.Properties.ObjectID]]
						.SetProperties(objectPropertiesEvent.Properties);
				}
			}
		}

		while (_objectPropertiesUpdateEvents.Count > 0)
		{
			if (_objectPropertiesUpdateEvents.TryDequeue(
				    out ObjectPropertiesUpdatedEventArgs objectPropertiesUpdateEvent))
			{
				if (scenePrims.ContainsKey(scenePrimIndexUUID[objectPropertiesUpdateEvent.Properties.ObjectID]))
				{
					scenePrims[scenePrimIndexUUID[objectPropertiesUpdateEvent.Properties.ObjectID]]
						.SetProperties(objectPropertiesUpdateEvent.Properties);
				}
			}
		}
	}

	private void ObjectPropertiesEventHandler(object sender, ObjectPropertiesEventArgs e)
	{
		if (scenePrims.TryGetValue(scenePrimIndexUUID[e.Properties.ObjectID], out ScenePrimData sPrim))
		{
			_objectPropertiesEvents.Enqueue(e);
		}
	}

	void ObjectDataBlockUpdateEvent(object sender, ObjectDataBlockUpdateEventArgs e)
	{
		objectDataBlockUpdates.Enqueue(e);
	}


	private void AvatarAnimationHandler(object sender, AvatarAnimationEventArgs e)
	{
		var map = this.meshObjectManager.avatarPrimitiveMap;
		if (map.TryGetValue(e.AvatarID, out Primitive prim))
		{
			foreach (OpenMetaverse.Animation anim in e.Animations)
			{
				if (SkeletonManager.HasClip(anim.AnimationID))
				{
					_loadedAnimationRequest.Enqueue(new Tuple<Primitive, UUID>(prim, anim.AnimationID));
				}
				else
				{
					ClientManager.assetManager.RequestAnimation(prim, anim.AnimationID);
				}
			}
		}
	}

	private void KillObjectEventHandler(object sender, KillObjectEventArgs e)
	{
		_killObjectQueue.Enqueue(new KillObjectEventData(sender, e));
	}

	private void KillObjectsEventHandler(object sender, KillObjectsEventArgs e)
	{
		foreach (uint id in e.ObjectLocalIDs)
		{
			_killObjectQueue.Enqueue(new KillObjectEventData(sender, new KillObjectEventArgs(e.Simulator, id)));
		}
	}

	private void KillObjects()
	{
		Material dissolver = Resources.Load<Material>("Dissolver");
		Material original;
		Material dissolvemat;
		List<MeshRequestData> newmrd = new();

		while (_killObjectQueue.Count > 0)
		{
			if (_killObjectQueue.TryDequeue(out KillObjectEventData data))
			{
				if (scenePrims.ContainsKey(data.e.ObjectLocalID))
				{
					if (orphanedPrims.ContainsKey(data.e.ObjectLocalID))
					{
						orphanedPrims.Remove(data.e.ObjectLocalID);
					}

					if (scenePrims[data.e.ObjectLocalID].obj == null) continue;
					if (scenePrims[data.e.ObjectLocalID].prim.RegionHandle != data.e.Simulator.Handle)
					{
						// Disabling this because it's making the viewer lag
						// Debug.Log($"KILLOBJECTS: wrong simulator {scenePrims[data.e.ObjectLocalID].prim.RegionHandle} != {data.e.Simulator.Handle}");
						continue;
					}

					GameObject go = scenePrims[data.e.ObjectLocalID].obj;
					Renderer[] rs = go.GetComponentsInChildren<Renderer>();
					foreach (Renderer r in rs)
					{
						original = r.material;
						dissolvemat = Instantiate<Material>(dissolver);
						dissolvemat.SetTexture("_MainTex", original.GetTexture("_MainTex"));
						r.material = dissolvemat;
						r.gameObject.AddComponent<DissolveOut>();
						r.GetComponent<MeshCollider>().enabled = false;
					}

					newmrd = new List<MeshRequestData>();
					foreach (MeshRequestData mrd in meshRequests)
					{
						if (mrd.meshHolder.gameObject != null) newmrd.Add(mrd);
					}

					meshRequests = newmrd;

					StartCoroutine(DissolveObject(scenePrims[data.e.ObjectLocalID].obj));
					if (scenePrims.ContainsKey(data.e.ObjectLocalID))
						scenePrims.Remove(data.e.ObjectLocalID, out ScenePrimData spd);

					//dissolveObject = DissolveObject(data);
				}
			}
		}
	}

	private IEnumerator DissolveObject(GameObject go)
	{
		yield return new WaitForSeconds(10f);
		if (go != null)
			if (!go.IsDestroyed())
				Destroy(go.transform.parent.gameObject);
	}

#if USE_FUNLY_SKY
	public TimeOfDayController timeOfDayController;
#endif

	// can we set a stopwatch to time all of these functions,
	// and then generate a debug report at the end?
	public void Update()
	{
		// ((ObjectProximityHandler)objectProximityHandler).DrawFrustum();// for debugging

		KillObjects();
		ScanForFaces();
		if (!ClientManager.active) return;
		float t = Time.deltaTime;

		ServiceReadyMeshQueue();
		ServiceReadyAnimationQueue();
		AllTextureRequests();
		if (!_config.UseNewObjectGraph)
		{
			// big %
			AllObjectsUpdate(t);
			ObjectBlockUpdates(t);
			TerseUpdates(t);
		}

		NameReplyEvents();
		ObjectPropertyEvents();
		AvatarUpdates();

		if (_updateSimContainers) StartCoroutine(UpdateContainers());

		//if (ClientManager.client.Grid.SunDirection.ToVector3()!=Vector3.zero)sun.forward = -ClientManager.client.Grid.SunDirection.ToVector3();
		//ClientManager.chat.log.text = $"SunPhase: {Mathf.Repeat((ClientManager.client.Grid.SunPhase * 0.15915494309189533576888376337251f) + 0.25f, 1f)}";
		var sunPhase = ClientManager.client?.Grid?.SunPhase ?? 0f;
#if USE_FUNLY_SKY
        timeOfDayController.skyTime = Mathf.Repeat((sunPhase * 0.15915494309189533576888376337251f) + 0.25f, 1f);
#else
		// TODO - Generic Sun Movement
		//sun.transform.forward = ClientManager.client.Grid.SunDirection.ToVector3();
#endif

		//TranslateObjects(t);

		if (_config.UseNewObjectGraph)
		{
			ServiceNewObjectQueue();
			ServiceSceneObjectsNeedingRenderersQueue();
		}
	}

	private bool _updateSimContainers = true;

	// we need to find a better way to update the terrains, callback is called too many times
	// iparfor the vert data
	private IEnumerator UpdateContainers()
	{
		_updateSimContainers = false;
		while (true)
		{
			foreach (var pair in simulators)
			{
				pair.Value.terrain.TerrainUpdate(pair.Value);
				pair.Value.age += Time.deltaTime;
				yield return null;
			}

			yield return new WaitForSeconds(0.5f);
		}

	}

	/// <summary>
	/// Move and rotate objects according to their velocity variables.
	/// </summary>
	/// <param name="t"></param>
	public List<uint> movingObjects = new();

	/// <summary>
	/// Move and rotate objects according to their velocity variables.
	/// </summary>
	/// <param name="t"></param>
	void TranslateObjects(float t)
	{
		foreach (uint localID in movingObjects)
		{
			try
			{
				if (scenePrims[localID].velocity != Vector3.zero || scenePrims[localID].omega != Vector3.zero)
				{
					scenePrims[localID].TranslateObject(t);
				}
			}
			catch
			{
				//deletedprims.Enqueue(localID);
			}
		}
	}

	/// <summary>
	/// This is supposed to update the camera's position, direction, and view distance for the
	/// server to know what objects to send to the client. However, it doesn't seem to work.
	/// </summary>
	IEnumerator UpdateCamera()
	{
		while (true)
		{
			if (client.Settings.SEND_AGENT_UPDATES && ClientManager.active)
			{
				//client.Self.Movement.Camera.SetPositionOrientation(new OMVVector3(cameraRoot.transform.position.x, cameraRoot.transform.position.z, cameraRoot.transform.position.y), cameraRoot.transform.rotation.eulerAngles.x * DEG_TO_RAD, cameraRoot.transform.rotation.eulerAngles.z * DEG_TO_RAD, cameraRoot.transform.rotation.eulerAngles.y * DEG_TO_RAD);
				Vector3 lookat =
					Camera.main.transform.position +
					(Camera.main.transform.forward *
					 7.5f); //cameraRoot.position + (cameraRoot.transform.forward * 7.5f);
				client.Self.Movement.Camera.LookAt(
					new OMVVector3(Camera.main.transform.position.x, Camera.main.transform.position.z,
						Camera.main.transform.position.y),
					new OMVVector3(lookat.x, lookat.z, lookat.y));
				objectProximityHandler.UpdateCameraProperties(Camera.main);

				//    client.Self.SimPosition + new OMVVector3(-5, 0, 0) * client.Self.Movement.BodyRotation,
				//    client.Self.SimPosition
				//);
				client.Self.Movement.Camera.Far = ClientManager.viewDistance;
				client.Self.SetHeightWidth((ushort)Screen.width, (ushort)Screen.height);
				client.Self.Movement.SetFOVVerticalAngle(Camera.main.fieldOfView * DEG_TO_RAD);
				client.Self.Movement.SendUpdate();
				//cameraRoot.transform.rotation = client.Self.Movement.BodyRotation.ToUnity();
				//cameraRoot.transform.position = client.Self.Movement.Camera.Position.ToUnity();
				//client.Self.
				//client.Self.Movement.Camera.LookDirection(new OMVVector3(cameraRoot.transform.forward.x, cameraRoot.transform.forward.z, cameraRoot.transform.forward.y);
				//client.Self.Movement.Camera.UpAxis = new OMVVector3(cameraRoot.transform.up.x, cameraRoot.transform.up.z, cameraRoot.transform.up.y);
				//client.Self.Movement.Camera.LeftAxis = new OMVVector3(-cameraRoot.transform.right.x, -cameraRoot.transform.right.z, -cameraRoot.transform.right.y);
			}

			/*
			string s = "";
			s += "IMeshRequestQueue" + Services.GetService<IMeshRequestQueue>().Count + "\n";
			s += "IMeshDownloadRequestQueue" + Services.GetService<IMeshDownloadRequestQueue>().Count + "\n";
			s += "IDownloadMeshCacheQueue" + Services.GetService<IDownloadedMeshCacheQueue>().Count + "\n";
			s += "IDownloadMeshQueue" + Services.GetService<IDownloadedMeshQueue>().Count + "\n";
			s += "IDecodedMeshQueue: " + Services.GetService<IDecodedMeshQueue>().Count + "\n";
			Debug.Log(s);
			*/

			yield return new WaitForSeconds(_config.updateCameraInterval);
		}
	}


	IEnumerator TextureRequests()
	{
		while (true)
		{
			var totalAllowedInOneFrame = 10;
			while (_readyTextureQueue.TryDequeue(out var textureItem) && textureItem is not null &&
			       totalAllowedInOneFrame-- > 0)
			{
				ClientManager.assetManager.MainThreadTextureReinitialize(textureItem.Data, textureItem.UUID,
					textureItem.Width, textureItem.Height, textureItem.Components);
			}

			//Perf.Measure("Resources.UnloadUnusedAssets", Resources.UnloadUnusedAssets);
			yield return Wait100ms;
		}
	}

	void AllTextureRequests()
	{
		var totalAllowedInOneFrame = 10;
		while (_readyTextureQueue.TryDequeue(out var textureItem) && textureItem is not null &&
		       totalAllowedInOneFrame-- > 0)
		{
			ClientManager.assetManager.MainThreadTextureReinitialize(textureItem.Data, textureItem.UUID,
				textureItem.Width, textureItem.Height, textureItem.Components);
		}
	}

	void ServiceReadyAnimationQueue()
	{
		ServiceQueueRepeatedly(_decodedAnimationQueue, this.meshObjectManager.SetupAnimation);

		// TODO: Refractor this maybe?
		while (_loadedAnimationRequest.TryDequeue(out var animationItem) && animationItem is not null)
		{
			this.meshObjectManager.SetupAnimation(animationItem.Item2, animationItem.Item1);
		}
	}


	/// <summary>
	/// Called once per fixed update.
	/// Services the ReadyMeshQueue
	/// </summary>
	private void ServiceReadyMeshQueue()
	{
		ServiceQueueRepeatedly(_decodedMeshQueue, this.meshObjectManager.SetupMeshObject);
	}

	/// <summary>
	/// Tries to perform action on items in queue until the queue is empty,
	/// or the limit is reached.
	/// </summary>
	/// <typeparam name="TQueueItem"></typeparam>
	/// <param name="queue"></param>
	/// <param name="action"></param>
	private void ServiceQueueRepeatedly<TQueueItem>(IConcurrentQueue<TQueueItem> queue, Action<TQueueItem> action)
	{
		uint limit = _config.LimitQueueItemsPerUpdateTo;
		uint count = 0;
		do
		{
			if (!queue.TryDequeue(out var item) || item is null) return;
			action(item);
		} while (count++ < limit);
	}

	/// <summary>
	/// Dequeues one mesh request and creates renderers for the mesh.
	/// </summary>
	/// <returns></returns>
	private bool DoOneDecodedMesh()
	{
		if (!_decodedMeshQueue.TryDequeue(out var item) || item is null) return false;
		// DoDecodedMesh(item);
		this.meshObjectManager.SetupMeshObject(item);
		return true;
	}


	/// <summary>
	///BGO represents the unscaled parent, which is in the correct position and rotation but has a scale of Vector3.one
	///GO represents the place holder cube object for rendering. It is given the same position and rotation as the BGO.
	///Faces are built by the RezzedObject script located in the GO
	///Faces are added to BGO, not to GO, and given the same rotation and position as the GO, then locally scaled
	///to the prim.Scale.ToUnity() vector and then lastly parented to the BGO
	/// </summary>
	void AllObjectsUpdate(float t)
	{
		while (objectUpdates.TryDequeue(out var update) && update is not null)
		{
			if (update.IsNew && !scenePrims.ContainsKey(update.Prim.LocalID))
			{
				NewObject(update.Prim);
			}
			else if (scenePrims.ContainsKey(update.Prim.LocalID))
			{
				scenePrims[update.Prim.LocalID].UpdateObject(update, t);
			}
		}
	}

	void ObjectBlockUpdates(float t)
	{
		while (objectDataBlockUpdates.TryDequeue(out var update) && update is not null)
		{
			if (scenePrims.ContainsKey(update.Prim.LocalID))
			{
				scenePrims[update.Prim.LocalID].ObjectBlockUpdate(update, t);
			}
		}
	}

	// Pass the terrain update event to the correct simulator's terrain handler.
	public void TerrainEventHandler(object sender, LandPatchReceivedEventArgs e)
	{
		simulators[e.Simulator.Handle].terrain.TerrainPatchUpdate(e);
	}


	public ConcurrentQueue<TerseObjectUpdateEventArgs> terseUpdates = new();

	void Objects_TerseObjectUpdate(object sender, TerseObjectUpdateEventArgs e)
	{
		this.objectProximityHandler.AddPrimPositionID(e.Prim);
		terseUpdates.Enqueue(e);
	}

	void TerseUpdates(float t)
	{
		while (terseUpdates.TryDequeue(out var update) && update is not null)
		{
			if (scenePrims.ContainsKey(update.Update.LocalID))
			{
				scenePrims[update.Update.LocalID].TerseUpdate(update, t);
			}
		}
	}

	public ConcurrentQueue<PrimEventArgs> objectUpdates = new();
	public ObjectProximityHandler objectProximityHandler;

	void Objects_ObjectUpdate(object sender, PrimEventArgs _event)
	{
		objectProximityHandler.AddObject(_event);
		//objectUpdates.Enqueue(_event);
	}


	public class MeshRequestData
	{
		public uint localID;
		public UUID sculptTextureUUID;
		public GameObject meshHolder;

		public MeshRequestData(uint lid, UUID uuid, GameObject mh)
		{
			localID = lid;
			sculptTextureUUID = uuid;
			meshHolder = mh;
		}
	}

	public static void PreTextureFace(Primitive prim, int j, Renderer rendr)
	{
		TextureEntryFace tef = prim.Textures.GetFace((uint)j);
		UUID uuid = tef.TextureID;
		Color color = tef.RGBA.ToUnity();

		if (!ClientManager.assetManager.materialContainer.ContainsKey(uuid))
		{
			ClientManager.assetManager.materialContainer.Add(uuid,
				new MaterialContainer(uuid, Texture2D.Instantiate(Texture2D.whiteTexture), 3));
			if (!ClientManager.assetManager.materials.ContainsKey(uuid))
			{
				ClientManager.assetManager.materials.Add(uuid, new List<Renderer>());
			}

			ClientManager.assetManager.materials[uuid].Add(rendr);
		}

		DissolveIn dis = rendr.gameObject.AddComponent<DissolveIn>();
		dis.texture = ClientManager.assetManager.materialContainer[uuid].texture;
		dis.color = color;
		dis.newMat = ClientManager.assetManager.materialContainer[uuid].GetMaterial(color, tef.Glow, tef.Fullbright);
	}

	public void TextureFace(Primitive prim, int subMeshIndex, Renderer rendr)
	{
		if (prim.Textures == null)
		{
			Debug.LogError("Prim: " + null + " Textures : " + prim.Textures + " Type: " + prim.Type +
			               " subMeshIndex: " + subMeshIndex);
		}

		TextureEntryFace textureEntryFace = prim.Textures.GetFace((uint)subMeshIndex);
		textureEntryFace.GetOSD(subMeshIndex);

		Color color = textureEntryFace.RGBA.ToUnity();

		Material newMaterial = ClientManager.assetManager.RequestTexture(textureEntryFace.TextureID, rendr,
			subMeshIndex, color, textureEntryFace.Glow, textureEntryFace.Fullbright);


		if (textureEntryFace.Fullbright) rendr.gameObject.layer = 7;
		else rendr.gameObject.layer = 0;

		if (scenePrims.ContainsKey(prim.LocalID))
		{
			if (IsHUD(scenePrims[prim.LocalID].prim))
			{
				rendr.gameObject.layer = 8;
			}

			if (scenePrims.ContainsKey(scenePrims[prim.LocalID].prim.ParentID))
			{
				if (IsHUD(scenePrims[scenePrims[prim.LocalID].prim.ParentID].prim))
				{
					rendr.gameObject.layer = 8;
				}
			}
		}
        else if (_config.UseNewObjectGraph && _renderManager != null && _renderManager.SceneObjects.Get(prim.LocalID) != null)
        {
            var sceneObj = _renderManager.SceneObjects.Get(prim.LocalID);
            if (IsHUD(sceneObj.SimObject.Prim))
            {
                rendr.gameObject.layer = 8;
            }
            
            if (sceneObj.SimObject.ParentID != 0)
            {
                var parent = _renderManager.SceneObjects.Get(sceneObj.SimObject.ParentID);
                if (parent != null && IsHUD(parent.SimObject.Prim))
                {
                    rendr.gameObject.layer = 8;
                }
            }
        }
	}

	private void ScanForFaces()
	{
		if (!ClientManager.active && Time.frameCount % 20 != 0) return;

		var unProcessed = new List<ScenePrimData>();
		while (unTexturedPrims.TryDequeue(out var kvp))
		{
			if (kvp.obj == null) continue;
			var pis = kvp.obj.transform.GetChild(0).GetComponentsInChildren<PrimInfo>();
			// kvp.obj.name = "TEXTURED " + kvp.obj.name + " " + pis.Length + " " +kvp.obj.transform.GetChild(0);
			if (pis.Length == 0)
			{
				unProcessed.Add(kvp);
				continue;
			}

			foreach (PrimInfo _pi in pis)
			{
				kvp.obj.name += " " + _pi.face;
				//only request texture if face is the same face and if it's not already textured
				if (!_pi.isTextured && _pi.face > -1)
				{
					//validate++;
					_pi.isTextured = true;
					try
					{
						TextureFace(_pi.prim, _pi.face, _pi.GetComponent<Renderer>());
						// unTexturedPrims.Remove(kvp.uuid);
					}
					catch (Exception e)
					{
						Debug.LogWarning(e);
					}
				}
			}
		}

		foreach (var pi in unProcessed)
		{
			unTexturedPrims.Enqueue(pi);
		}
	}

	private void ServiceSceneObjectsNeedingRenderersQueue()
	{
		ServiceQueueRepeatedly(_needRenderDataQueue, SetupRenderDataForObject);
	}

	// Replaced by SetupRenderDataForObject
	/*private void DoSceneObjectNeedingRenderer(SceneObject obj)
	{
		_log.Render_SceneObjectNeedsRenderers(obj.LocalID);

		switch (obj.SimObject.PrimType)
		{
			case PrimType.Unknown:
				// Doesn't seem we can do anything with this.
				break;
			case PrimType.Sculpt:
				// TODO RequestSculpt()
				break;
			case PrimType.Mesh:
				// TODO RequestMesh()
				break;
			default:
				// TODO RequestGeneratedMesh()
				break;
		}

		// TODO Setup Lights

		// TODO Setup Particles.
	}*/

	/// <summary>
	/// Sets up Render Data for objects that need it.
	/// </summary>
	private void ServiceNeedRenderDataQueue()
	{
		ServiceQueueRepeatedly(_needRenderDataQueue, SetupRenderDataForObject);
	}

	private void SetupRenderDataForObject(SceneObject sceneObject)
	{
		RemoveRenderers(sceneObject);

		var simObject = sceneObject.SimObject;

		if (!simObject.IsAttachment &&
		    simObject.SimVelocity == Vector3.zero &&
		    simObject.SimAngularVelocity == Vector3.zero)
		{
			// This will cause the sim to send back an ObjectPropertiesPacket
			ClientManager.client.Objects.SelectObject(
				ClientManager.client.Network.CurrentSim,
				simObject.LocalID);
			// Why is this being done here? -Kage
		}

		switch (simObject.PrimType)
		{
			case PrimType.Sculpt:
				SetupSculptRenderer(sceneObject);
				break;
			case PrimType.Unknown:
				// Nothing we can really do with this, so just ignore it for now.
				break;
			case PrimType.Mesh:
				SetupMeshRenderer(sceneObject);
				break;
			default:
				SetupClassicPrimRenderer(sceneObject);
				break;
		}

		SetupLight(sceneObject);

		SetupParticles(sceneObject);
	}

	private void SetupParticles(SceneObject sceneObject)
	{
		var prim = sceneObject.SimObject.Prim;
		if (prim.ParticleSys.Pattern == Primitive.ParticleSystem.SourcePattern.None && sceneObject.GameObject.GetComponent<UnityEngine.ParticleSystem>() == null) return;

		var obj = sceneObject.GameObject;
		bool hasParticles = obj.GetComponent<UnityEngine.ParticleSystem>() != null;
		
		if (prim.ParticleSys.Pattern != Primitive.ParticleSystem.SourcePattern.None)
		{
			UnityEngine.ParticleSystem ps = obj.GetComponent<UnityEngine.ParticleSystem>();
			if(ps == null) ps = obj.AddComponent<UnityEngine.ParticleSystem>();

			if (!hasParticles) ps.Stop();
			var main = ps.main;

			AnimationCurve burstSpeedCurve = new();
			burstSpeedCurve.AddKey(prim.ParticleSys.BurstSpeedMin, 0f);
			burstSpeedCurve.AddKey(prim.ParticleSys.BurstSpeedMax, 1f);
			main.startSpeed = new UnityEngine.ParticleSystem.MinMaxCurve(1f, burstSpeedCurve);

			main.startLifetime = prim.ParticleSys.PartMaxAge;
			
			if (!hasParticles)
			{
				if (prim.ParticleSys.MaxAge != 0f)
				{
					main.loop = false;
					main.duration = prim.ParticleSys.MaxAge;
				}
			}

			var em = ps.emission;
			em.enabled = true;
			em.rateOverTime = prim.ParticleSys.BurstPartCount / prim.ParticleSys.BurstRate;

			var fo = ps.forceOverLifetime;

			fo.enabled = true;
			Vector3 vec = prim.ParticleSys.PartAcceleration.ToVector3();
			
			var scale = obj.transform.lossyScale;
			if (Mathf.Abs(scale.x) > 0.001f) fo.x = vec.x / scale.x;
			if (Mathf.Abs(scale.y) > 0.001f) fo.y = vec.y / scale.y;
			if (Mathf.Abs(scale.z) > 0.001f) fo.z = vec.z / scale.z;

			vec = prim.ParticleSys.AngularVelocity.ToVector3() * Mathf.Rad2Deg;

			var vel = ps.velocityOverLifetime;
			vel.enabled = true;

			vel.orbitalX = vec.x;
			vel.orbitalY = vec.y;
			vel.orbitalZ = vec.z;

			var sh = ps.shape;
			sh.enabled = true;
			float mag = scale.magnitude;
			sh.radius = mag > 0 ? (prim.ParticleSys.BurstRadius / mag) * 2f : 0;
			sh.rotation = new Vector3(-90f, 0f, 0f);

			ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
			r.material = Instantiate(ResourceCache.particleMaterial);
			r.material.SetTexture("_BaseMap", ClientManager.assetManager.RequestTexture(prim.ParticleSys.Texture));

			switch (prim.ParticleSys.Pattern)
			{
				case Primitive.ParticleSystem.SourcePattern.Angle:
					sh.shapeType = ParticleSystemShapeType.Circle;
					sh.arc = (prim.ParticleSys.OuterAngle - prim.ParticleSys.InnerAngle) * Mathf.Rad2Deg;
					break;
				case Primitive.ParticleSystem.SourcePattern.Drop:
					sh.shapeType = ParticleSystemShapeType.Sphere;
					sh.radius = 0f;
					main.startSpeed = 0f;
					break;
				case Primitive.ParticleSystem.SourcePattern.Explode:
					sh.shapeType = ParticleSystemShapeType.Sphere;
					sh.radiusThickness = 1f;
					sh.arc = 360f;
					break;
				case Primitive.ParticleSystem.SourcePattern.AngleCone:
					sh.shapeType = ParticleSystemShapeType.Cone;
					sh.angle = (prim.ParticleSys.OuterAngle - prim.ParticleSys.InnerAngle) * Mathf.Rad2Deg;
					break;
				case Primitive.ParticleSystem.SourcePattern.AngleConeEmpty:
					sh.shapeType = ParticleSystemShapeType.Sphere;
					sh.radius = 0f;
					main.startSpeed = 0f;
					break;
			}

			var col = ps.colorOverLifetime;
			col.enabled = true;
			Gradient grad = new();
			grad.SetKeys(
				new GradientColorKey[]
				{
						new GradientColorKey(prim.ParticleSys.PartStartColor.ToUnity(), 0f),
						new GradientColorKey(prim.ParticleSys.PartEndColor.ToUnity(), 0f)
				},
				new GradientAlphaKey[]
				{
						new GradientAlphaKey(prim.ParticleSys.PartStartColor.A, 0f),
						new GradientAlphaKey(prim.ParticleSys.PartEndColor.A, 1f)
				});
			col.color = grad;

			var sz = ps.sizeOverLifetime;
			sz.enabled = true;
			sz.separateAxes = true;
			AnimationCurve xcurve = new();
			xcurve.AddKey(Mathf.Abs(scale.x) > 0.001f ? prim.ParticleSys.PartStartScaleX / scale.x : 0, 0f);
			xcurve.AddKey(Mathf.Abs(scale.x) > 0.001f ? prim.ParticleSys.PartEndScaleX / scale.x : 0, 1f);
			AnimationCurve ycurve = new();
			ycurve.AddKey(Mathf.Abs(scale.y) > 0.001f ? prim.ParticleSys.PartStartScaleY / scale.y : 0, 0f);
			ycurve.AddKey(Mathf.Abs(scale.y) > 0.001f ? prim.ParticleSys.PartEndScaleY / scale.y : 0, 1f);
			AnimationCurve zcurve = new();
			zcurve.AddKey(Mathf.Abs(scale.z) > 0.001f ? 1f / scale.z : 1f, 0f);
			zcurve.AddKey(Mathf.Abs(scale.z) > 0.001f ? 1f / scale.z : 1f, 1f);
			
			sz.x = new UnityEngine.ParticleSystem.MinMaxCurve(1f, xcurve);
			sz.y = new UnityEngine.ParticleSystem.MinMaxCurve(1f, ycurve);
			sz.z = new UnityEngine.ParticleSystem.MinMaxCurve(1f, zcurve);

			if (prim.ParticleSys.PartDataFlags.HasFlag(Primitive.ParticleSystem.ParticleDataFlags.Bounce))
			{
				var co = ps.collision;
				co.enabled = true;
				co.quality = ParticleSystemCollisionQuality.Low;
				co.enableDynamicColliders = true;
				co.collidesWith = LayerMask.GetMask(new string[] { "Default", "Transparent", "Glow", "Terrain", "Water" });
				co.type = ParticleSystemCollisionType.World;
				co.mode = ParticleSystemCollisionMode.Collision3D;
				co.maxCollisionShapes = 10;
			}

			if (prim.ParticleSys.PartDataFlags.HasFlag(Primitive.ParticleSystem.ParticleDataFlags.FollowSrc))
			{
				main.simulationSpace = ParticleSystemSimulationSpace.Local;
			}
			else
			{
				main.simulationSpace = ParticleSystemSimulationSpace.World;
			}

			if (prim.ParticleSys.HasGlow())
			{
				obj.layer = 7;
			}
			else
			{
				obj.layer = 0;
			}

			if (!hasParticles) ps.Play();
		}
		else if (hasParticles)
		{
			Destroy(obj.GetComponent<UnityEngine.ParticleSystem>());
		}
	}

	/// <summary>
	/// Setup the scene object as a light source.
	/// </summary>
	/// <param name="sceneObject"></param>
	private void SetupLight(SceneObject sceneObject)
	{
		var simObject = sceneObject.SimObject;
		if (!simObject.IsLight) return;

		var golight = Instantiate(Resources.Load<GameObject>("Point Light"));
		sceneObject.Light = golight;

		golight.name = $"Light({simObject.LocalID})";
		golight.transform.parent = sceneObject.GameObject.transform;
		//children.Add(light);
		golight.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

		var lc = golight.GetComponent<Light>();
		lc.range = simObject.LightRadius;
		lc.color = simObject.LightColor;
		lc.intensity = simObject.LightIntensity;
	}

	private void SetupClassicPrimRenderer(SceneObject sceneObject)
	{
		if (sceneObject.MeshHolder == null) return;

		var prim = sceneObject.SimObject.Prim;
		if (prim.PrimData.PCode != PCode.Prim && prim.PrimData.PCode != PCode.Avatar) return;

		sceneObject.GameObject.GetComponent<Renderer>().enabled = false;
		LODGroup group = sceneObject.MeshHolder.GetComponent<LODGroup>();
		if (group == null) group = sceneObject.MeshHolder.AddComponent<LODGroup>();

		Renderer[] highest = GeneratePrimForSceneObject(sceneObject, DetailLevel.Highest);
		Renderer[] medium = GeneratePrimForSceneObject(sceneObject, DetailLevel.Medium);
		Renderer[] low = GeneratePrimForSceneObject(sceneObject, DetailLevel.Low);

		int totalLength = highest.Length + medium.Length + low.Length;
		sceneObject.Renderers = new Renderer[totalLength];
		highest.CopyTo(sceneObject.Renderers, 0);
		medium.CopyTo(sceneObject.Renderers, highest.Length);
		low.CopyTo(sceneObject.Renderers, highest.Length + medium.Length);

		LOD[] lods = new LOD[3]
		{
			new LOD(1.0f, highest),
			new LOD(0.5f, medium),
			new LOD(0.1f, low)
		};

		group.SetLODs(lods);
		group.fadeMode = LODFadeMode.SpeedTree;
		group.animateCrossFading = true;
		group.RecalculateBounds();
		group.size = 10f;

		if (IsHUD(prim)) group.enabled = false;
	}

	private Renderer[] GeneratePrimForSceneObject(SceneObject sceneObject, DetailLevel detailLevel)
	{
		var prim = sceneObject.SimObject.Prim;
		if (prim.PrimData.PCode == PCode.Avatar) return new Renderer[0];

		MeshmerizerR mesher = new();
		FacetedMesh fmesh = mesher.GenerateFacetedMesh(prim, detailLevel);

		GameObject gomesh;
		Renderer rendr;
		MeshFilter meshFilter;

		// Create the mesh object
		gomesh = Instantiate(ResourceCache.cubePrefab);
		gomesh.name = $"Combined Mesh {detailLevel}";
		gomesh.transform.parent = sceneObject.MeshHolder.transform;
		gomesh.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		gomesh.transform.localScale = Vector3.one;

		rendr = gomesh.GetComponent<Renderer>();
		rendr.enabled = true;
		meshFilter = gomesh.GetComponent<MeshFilter>();
		meshFilter.mesh = null;

		PrimInfo pi = gomesh.GetComponent<PrimInfo>();
		if (prim.LocalID == 0) Debug.Log("local ID cannot be 0");
		pi.localID = prim.LocalID;
		pi.uuid = prim.ID;

		List<Vector3> allVertices = new List<Vector3>();
		List<Vector3> allNormals = new List<Vector3>();
		List<Vector2> allUvs = new List<Vector2>();
		List<Material> materials = new List<Material>();
		List<int> subMeshIndices = new List<int>();

		int vertexOffset = 0;

		// We need _transformTexCoords service
		var transformTexCoords = Services.GetService<ITransformTexCoords>();

		for (int j = 0; j < fmesh.Faces.Count; j++)
		{
			var faceVertices = fmesh.Faces[j].Vertices;
			int vertexCount = faceVertices.Count;

			Vector3[] vertices = new Vector3[vertexCount];
			Vector3[] normals = new Vector3[vertexCount];
			Vector2[] uvs = new Vector2[vertexCount];

			Primitive.TextureEntryFace textureEntryFace = prim.Textures.GetFace((uint)j);

			transformTexCoords.TransformTexCoords(faceVertices, fmesh.Faces[j].Center, textureEntryFace, prim.Scale);

			for (int i = 0; i < vertexCount; i++)
			{
				vertices[i] = faceVertices[i].Position.ToUnity();
				normals[i] = faceVertices[i].Normal.ToUnity() * -1f;
				uvs[i] = faceVertices[i].TexCoord.ToUnity();
			}

			allVertices.AddRange(vertices);
			allNormals.AddRange(normals);
			allUvs.AddRange(uvs);

			int[] indices = fmesh.Faces[j].Indices.Select(index => index + vertexOffset).ToArray();
			subMeshIndices.AddRange(indices);

			vertexOffset += vertexCount;

			Color color = textureEntryFace.RGBA.ToUnity();
			Material clonemat;

			if (color.a < 0.999f)
			{
				clonemat = new Material(textureEntryFace.Fullbright ? ResourceCache.alphaFullbrightMaterial : ResourceCache.alphaMaterial);
			}
			else
			{
				clonemat = new Material(textureEntryFace.Fullbright ? ResourceCache.opaqueFullbrightMaterial : ResourceCache.opaqueMaterial);
			}

			clonemat.SetColor("_BaseColor", color);
			materials.Add(clonemat);
		}

		rendr.materials = materials.ToArray();

		Mesh mesh = new Mesh();
		mesh.SetVertices(allVertices);
		mesh.SetNormals(allNormals);
		mesh.SetUVs(0, allUvs);

		mesh.subMeshCount = fmesh.Faces.Count;

		int offset = 0;
		for (int j = 0; j < fmesh.Faces.Count; j++)
		{
			pi.face = j;
			int count = fmesh.Faces[j].Indices.Count;
			mesh.SetTriangles(subMeshIndices.GetRange(offset, count), j);
			offset += count;
			TextureFace(prim, j, rendr);
		}

		meshFilter.sharedMesh = mesh.ReverseWind().FlipNormals();

		return new Renderer[] { rendr };
	}

	private void SetupMeshRenderer(SceneObject sceneObject)
	{
		if (sceneObject.MeshHolder == null) return;
		ClientManager.assetManager.RequestMesh2(sceneObject.GameObject, sceneObject.SimObject.Prim, sceneObject.SimObject.Prim.Sculpt.SculptTexture, sceneObject.MeshHolder);
	}

	private void SetupSculptRenderer(SceneObject sceneObject)
	{
		if (sceneObject.MeshHolder == null) return;
		ClientManager.assetManager.RequestSculpt(sceneObject.MeshHolder, sceneObject.SimObject.Prim);
	}

	private void RemoveRenderers(SceneObject sceneObject)
	{
		if (sceneObject.Renderers == null) return; // sanity check

		// something here feels wrong. this method is based on code in the old ScenePrimData.Render()
		// It looks like it used to destroy the old renderer stuff, (and the KWS #if still does.)
		// but in recent versions instead of destroying it enables the render and the nulling of the
		// array was disabled? Something here doesn't add up to me. For now I will preseve the old
		// behavior.  -Kage

		foreach (var r in sceneObject.Renderers)
		{
			if (r != null)
			{
				r.enabled = true;
				//DestroyImmediate(r.gameObject); 
			}
		}
		//sceneObject.Renderers = null;

#if USE_KWS
		if (sceneObject.IsWater)
		{
			sceneObject.IsWater = false;
			SimManager.Destroy(sceneObject.WaterSystem);
			SimManager.Destroy(sceneObject.WaterBox);
			sceneObject.WaterSystem = null;
			sceneObject.WaterBox = null;
		}
#endif
	}

	/// <summary>
	/// Creates SceneObjects for SimObjects in the NewObjectQueue
	/// </summary>
	private void ServiceNewObjectQueue()
	{
		ServiceQueueRepeatedly(_newObjectQueue, DoNewSceneObject);
	}

	/// <summary>
	/// Creates the GameObjects in the scene, and sets up their relationships
	/// to other objects (such as setting transform parents.)
	/// The default Renderer will be disabled, with the expectation that a specific
	/// (Sculpt, Mesh, Geometic Prim) rendered will be added to the GameObjects later.
	/// </summary>
	/// <param name="simObject"></param>
	/// <exception cref="ApplicationException"></exception>
	private void DoNewSceneObject(SimObject simObject)
	{
		// don't bother with objects in other regions for now.
		if (_config.LimitToCurrentRegion) return;

		var localID = simObject.LocalID;

		var heirarchyHolder = Instantiate(blank);
		heirarchyHolder.name = $"Heirarch Holder ({localID})";

		var gameObject = Instantiate(cube);
		gameObject.name = $"Object ({localID})";
		gameObject.transform.parent = heirarchyHolder.transform;
		gameObject.transform.localPosition = Vector3.zero;

		// disable rendering of the newly created object.
		// Its currently a cube, and probably not what we
		// want to show anyway. 
		var meshRenderer = gameObject.GetComponent<MeshRenderer>();
		meshRenderer.enabled = false;

		var meshHolder = Instantiate(blank);
		meshHolder.name = $"Mesh Holder({localID})";
		meshHolder.transform.parent = heirarchyHolder.transform;

		var sceneObject = new SceneObject()
		{
			LocalID = simObject.LocalID,
			HeirachyHolder = heirarchyHolder,
			GameObject = gameObject,
			MeshHolder = meshHolder,
			SimObject = simObject,
		};

		if (!_renderManager.SceneObjects.Add(sceneObject))
		{
			// oh no, something when wrong. Lets clean up our mess.
			Destroy(heirarchyHolder);
		}

		gameObject.transform.localScale = simObject.Scale;

		// at this point a note was made of the HashCode of the primData.
		// It looks like its purpose is to know if the object has changed.
		//	scenePrims[prim.LocalID].ConstructionHash = prim.PrimData.GetHashCode();

		if (simObject.ParentID != 0)
		{
			// this object has a parent. 
			var sceneParent = _renderManager.SceneObjects.Get(simObject.ParentID);
			if (sceneParent is null)
			{
				// AllSimObjects is not supposed to put orphans into the newObjectQueue.
				// I'm not sure why this happening -Kage
				_log.Render_NewOrphanObject(simObject.LocalID, simObject.ParentID); // Warn
				_newObjectQueue.Enqueue(simObject); // Try again later.
			}

			if (simObject.IsHud())
			{
				int anchorIndex = (int)simObject.AttachmentPoint - 31;
				heirarchyHolder.transform.parent = hudAnchors[anchorIndex];
				heirarchyHolder.SetLayerRecursively(8);
			}
			else
			{
				heirarchyHolder.transform.parent = sceneParent.GameObject.transform.parent;
			}
		}

		heirarchyHolder.transform.SetLocalPositionAndRotation(
			simObject.SimPosition,
			simObject.SimRotation);

		// at this point in the old object management, the render method would have been called.
		// that render method is kind of hard to follow, but really the next step is to setup Renderes on the objects,
		// which means creating an Array of UnityEngine.Renderer from Generated classic prim data, downloaded mesh data
		// or downloaded sculpt data.

		_needRenderDataQueue.Enqueue(sceneObject);
	}

	static class ProfUtil
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long TS() => Stopwatch.GetTimestamp();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double MS(long start, long end) =>
			(end - start) * 1000.0 / Stopwatch.Frequency;
	}

	private void NewObject(Primitive prim)
	{
		//if (prim.RegionHandle != thissim) return;

		DebugStatsManager.AddStateUpdate(DebugStatsType.NewPrim, prim.Type.ToString());

		var bgo = Instantiate(blank);

		bgo.transform.parent = simulators[prim.RegionHandle].transform;

		bgo.name = $"object {prim.LocalID} heirarchy holder";

		var go = Instantiate<GameObject>(cube);
		go.name = $"object {prim.LocalID}";

		var meshHolder = Instantiate<GameObject>(blank);
		go.transform.parent = bgo.transform;
		go.transform.localPosition = Vector3.zero;
		meshHolder.transform.parent = bgo.transform;


		ScenePrimData sPD = new(go, prim);

		if (prim.Type == PrimType.Sculpt || prim.Type == PrimType.Mesh) unTexturedPrims.Enqueue(sPD);


		if (scenePrims.TryAdd(prim.LocalID, sPD))
		{
			go.transform.localScale = prim.Scale.ToUnity();
			scenePrims[prim.LocalID].uuid = prim.ID;
			scenePrims[prim.LocalID].ConstructionHash = prim.PrimData.GetHashCode();
			if (prim.ParentID != 0)
			{
				if (!scenePrims.ContainsKey(prim.ParentID))
				{
					orphanedPrims.TryAdd(prim.ParentID, new List<Primitive>());
					orphanedPrims[prim.ParentID].Add(prim);
					go.transform.position = new Vector3(5000f, 5000f, 5000f);
				}
				else
				{
					if (!IsHUD(prim))
					{
						bgo.transform.parent = scenePrims[prim.ParentID].obj.transform.parent;
						if (IsHUD(scenePrims[prim.ParentID].prim))
						{
							scenePrims[prim.LocalID].parentIsHUD = true;
						}
					}
					else
					{
						bgo.transform.parent = hudAnchors[(int)prim.PrimData.AttachmentPoint - 31];
						bgo.SetLayerRecursively(8);
					}
				}
			}


			if (IsHUD(prim))
			{
				bgo.transform.SetLocalPositionAndRotation(
					GetHUDPosition(prim),
					prim.Rotation.ToUnity());
			}
			else
			{
				HandleAttachment(bgo, prim);
				bgo.transform.SetLocalPositionAndRotation(
					prim.Position.ToUnity(),
					prim.Rotation.ToUnity());
			}

			if (orphanedPrims.ContainsKey(prim.ParentID))
			{
				foreach (Primitive p in orphanedPrims[prim.ParentID])
				{
					if (p.ParentID == prim.ParentID && scenePrims.ContainsKey(prim.ParentID))
					{
						scenePrims[p.LocalID].obj.transform.parent.parent =
							scenePrims[prim.ParentID].obj.transform.parent;
						bgo.transform.SetLocalPositionAndRotation(
							prim.Position.ToUnity(),
							prim.Rotation.ToUnity());

						if (IsHUD(scenePrims[prim.ParentID].prim))
						{
							scenePrims[p.LocalID].parentIsHUD = true;
						}
					}
				}
			}

			scenePrims[prim.LocalID].Render();
		}
		else
		{
			if (scenePrims.ContainsKey(prim.LocalID))
			{
				Debug.LogWarning($"Tried to create new prim, but localID:{prim.LocalID} already exists in scene.");
			}
			else
			{
				Debug.LogWarning($"Error adding prim to dictionary.");
			}

			Destroy(bgo);
		}
	}

	private void AddPositionAndRotationConstraints(GameObject obj, Transform anchor, Vector3 posOffset,
		Vector3 rotOffset)
	{
		// Add Position Constraint
		PositionConstraint positionConstraint = obj.AddComponent<PositionConstraint>();

		ConstraintSource positionSource = new ConstraintSource();
		positionSource.sourceTransform = anchor;
		positionSource.weight = 1;

		positionConstraint.AddSource(positionSource);
		positionConstraint.translationOffset = posOffset;
		positionConstraint.constraintActive = true;
		positionConstraint.locked = true;

		// Add Rotation Constraint
		RotationConstraint rotationConstraint = obj.AddComponent<RotationConstraint>();

		ConstraintSource rotationSource = new ConstraintSource();
		rotationSource.sourceTransform = anchor;
		rotationSource.weight = 1;

		rotationConstraint.AddSource(rotationSource);
		rotationConstraint.rotationOffset = rotOffset;
		rotationConstraint.constraintActive = true;
		rotationConstraint.locked = true;
	}

	private Transform FindChildByNameRecursive(Transform parent, string name)
	{
		foreach (Transform child in parent)
		{
			if (child.name == name)
			{
				return child;
			}

			Transform result = FindChildByNameRecursive(child, name);
			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	private void CleanOrphanedPrims(Primitive prim)
	{
		if (orphanedPrims.ContainsKey(prim.ParentID))
		{
			orphanedPrims[prim.ParentID].Remove(prim);
			if (orphanedPrims[prim.ParentID].Count == 0)
			{
				orphanedPrims.Remove(prim.ParentID);
			}
		}
	}
}
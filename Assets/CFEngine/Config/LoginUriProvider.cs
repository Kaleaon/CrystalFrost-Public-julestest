using Microsoft.Extensions.Options;

namespace CrystalFrost.Config
{
	/// <summary>
	/// Defines the interface for a login URI provider.
	/// </summary>
	public interface ILoginUriProvider
	{
		/// <summary>
		/// Gets the login URI.
		/// </summary>
		/// <returns>The login URI.</returns>
		string GetLoginUri();
	}

	/// <summary>
	/// Provides the login URI.
	/// </summary>
	public class LoginUriProvider : ILoginUriProvider
	{

		/// <inheritdoc/>
		public string GetLoginUri()
		{
            // TODO: add the grid stuff
            // For the moment, just use the value that was set in the config
            var gridConfig = Services.GetService<IOptions<GridConfig>>().Value;
            return gridConfig.LoginURI;
		}
	}
}

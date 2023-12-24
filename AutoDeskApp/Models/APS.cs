using Autodesk.Forge;
using Autodesk.Forge.Model;

namespace AutoDeskApp.Models;

public class APS
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _callbackUri;
    private readonly Scope[] InternalTokenScopes = new Scope[] { Scope.DataRead, Scope.ViewablesRead };
    private readonly Scope[] PublicTokenScopes = new Scope[] { Scope.ViewablesRead };

    public APS(string clientId, string clientSecret, string callbackUri)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _callbackUri = callbackUri;
    }

    public string GetAuthorizationURL()
    {
        return new ThreeLeggedApi().Authorize(_clientId, "code", _callbackUri, InternalTokenScopes);
    }

    public async Task<Tokens> GenerateTokens(string code)
    {
        dynamic internalAuth = await new ThreeLeggedApi().GettokenAsync(_clientId, _clientSecret, "authorization_code", code, _callbackUri);
        dynamic publicAuth = await new ThreeLeggedApi().RefreshtokenAsync(_clientId, _clientSecret, "refresh_token", internalAuth.refresh_token, PublicTokenScopes);
        return new Tokens
        {
            PublicToken = publicAuth.access_token,
            InternalToken = internalAuth.access_token,
            RefreshToken = publicAuth.refresh_token,
            ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds(internalAuth.expires_in)
        };
    }

    public async Task<Tokens> RefreshTokens(Tokens tokens)
    {
        dynamic internalAuth = await new ThreeLeggedApi().RefreshtokenAsync(_clientId, _clientSecret, "refresh_token", tokens.RefreshToken, InternalTokenScopes);
        dynamic publicAuth = await new ThreeLeggedApi().RefreshtokenAsync(_clientId, _clientSecret, "refresh_token", internalAuth.refresh_token, PublicTokenScopes);
        return new Tokens
        {
            PublicToken = publicAuth.access_token,
            InternalToken = internalAuth.access_token,
            RefreshToken = publicAuth.refresh_token,
            ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds(internalAuth.expires_in)
        };
    }

    public async Task<dynamic> GetUserProfile(Tokens tokens)
    {
        var api = new UserProfileApi();
        api.Configuration.AccessToken = tokens.InternalToken;
        dynamic profile = await api.GetUserProfileAsync();
        return profile;
    }

    public async Task<IEnumerable<dynamic>> GetHubs(Tokens tokens)
    {
        var hubs = new List<dynamic>();
        var api = new HubsApi();
        api.Configuration.AccessToken = tokens.InternalToken;
        var response = await api.GetHubsAsync();
        foreach (KeyValuePair<string, dynamic> hub in new DynamicDictionaryItems(response.data))
        {
            hubs.Add(hub.Value);
        }
        return hubs;
    }

    public async Task<IEnumerable<dynamic>> GetProjects(string hubId, Tokens tokens)
    {
        var projects = new List<dynamic>();
        var api = new ProjectsApi();
        api.Configuration.AccessToken = tokens.InternalToken;
        var response = await api.GetHubProjectsAsync(hubId);
        foreach (KeyValuePair<string, dynamic> project in new DynamicDictionaryItems(response.data))
        {
            projects.Add(project.Value);
        }
        return projects;
    }

    public async Task<IEnumerable<dynamic>> GetContents(string hubId, string projectId, string folderId, Tokens tokens)
    {
        var contents = new List<dynamic>();
        if (string.IsNullOrEmpty(folderId))
        {
            var api = new ProjectsApi();
            api.Configuration.AccessToken = tokens.InternalToken;
            var response = await api.GetProjectTopFoldersAsync(hubId, projectId);
            foreach (KeyValuePair<string, dynamic> folders in new DynamicDictionaryItems(response.data))
            {
                contents.Add(folders.Value);
            }
        }
        else
        {
            var api = new FoldersApi();
            api.Configuration.AccessToken = tokens.InternalToken;
            var response = await api.GetFolderContentsAsync(projectId, folderId); // TODO: add paging
            foreach (KeyValuePair<string, dynamic> item in new DynamicDictionaryItems(response.data))
            {
                contents.Add(item.Value);
            }
        }
        return contents;
    }

    public async Task<IEnumerable<dynamic>> GetVersions(string hubId, string projectId, string itemId, Tokens tokens)
    {
        var versions = new List<dynamic>();
        var api = new ItemsApi();
        api.Configuration.AccessToken = tokens.InternalToken;
        var response = await api.GetItemVersionsAsync(projectId, itemId);
        foreach (KeyValuePair<string, dynamic> version in new DynamicDictionaryItems(response.data))
        {
            versions.Add(version.Value);
        }
        return versions;
    }
}

namespace Stemma.Redis
{
    public interface IRedisService
    {
        Task<List<KeyValuePair<string, int>>> GetLangFromUserAsync(string UserName);
        Task<int> SetLangForUserAsync(string UserName, Dictionary<string,int> langDic);
    }
}

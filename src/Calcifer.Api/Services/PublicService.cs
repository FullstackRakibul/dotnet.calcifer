
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Calcifer.Api.Interface.Common;

namespace Calcifer.Api.Services
{
    public class PublicService : IPublicInterface
    {
        public async Task<string> GetPublicDataAsync()
        {
            return "This is Public Service ......working...";
        }
    }
}

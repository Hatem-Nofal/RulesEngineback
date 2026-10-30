using System.Threading.Tasks;
using Volo.Abp.AspNetCore.SignalR;

namespace RulesEngine.Notifications
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class NotificationHub : AbpHub
    {
        //private readonly UserHubConnections _userConnectionsSingleton;

        //public NotificationHub(UserHubConnections userConnectionsSingleton)
        //{
        //    _userConnectionsSingleton = userConnectionsSingleton;
        //}

        public Task<bool> CheckConnction()
        {
            return Task.FromResult(true);
        }


    }
}

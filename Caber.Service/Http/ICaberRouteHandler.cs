using System.Threading.Tasks;

namespace Caber.Service.Http
{
    public interface ICaberRouteHandler
    {
        Task ExecuteAsync(CaberRequestContext context);
    }
}

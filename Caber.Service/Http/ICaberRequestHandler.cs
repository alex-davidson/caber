using System.Threading.Tasks;

namespace Caber.Service.Http
{
    public interface ICaberRequestHandler
    {
        Task HandleAsync(CaberRequestContext context);
    }
}

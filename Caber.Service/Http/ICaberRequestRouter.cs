namespace Caber.Service.Http
{
    public interface ICaberRequestRouter
    {
        CaberRouteData Route(CaberRequestContext context);
    }
}

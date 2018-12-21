namespace Caber.Service.Http.Authentication
{
    public enum CaberMutualAuthenticationFailureReason
    {
        None = 0,
        MissingOrInvalidClientUUID,
        MissingOrInvalidServerUUID,
        ServerUUIDDoesNotReferToThisInstance,
        NoClientCertificateProvided,
        ClientCertificateDoesNotMatchAnyKnownForTheClaimedUUID
    }
}

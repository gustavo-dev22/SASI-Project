namespace SASI.Dominio.Modelo
{
    public enum PrioridadIncidencia
    {
        Baja = 0,
        Media = 1,
        Alta = 2,
        Critica = 3
    }

    public enum EstadoIncidencia
    {
        Abierta = 0,
        EnProceso = 1,
        Resuelta = 2,
        Cerrada = 3
    }

    public enum EstadoSolicitudAcceso
    {
        Pendiente = 0,
        Aprobada = 1,
        Rechazada = 2
    }

    public enum EstadoOperativo
    {
        Operativo = 0,
        Incidente = 1,
        Mantenimiento = 2
    }
}

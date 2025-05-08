using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<List<ClientTripDTO>> GetClientTrips(int id);
    Task<int> CreateClient(ClientDTO client);
    Task RegisterClientForTrip(int idClient, int idTrip);
    Task UnregisterClientFromTrip(int idClient, int idTrip);
    Task<bool> DoesTripExist(int id);
    Task<bool> DoClientTripsExist(int id);
    Task<bool> DoesClientExist(int id);
    Task<bool> DoesClientPeselExist(string pesel);
    Task<bool> IsClientRegisteredForTrip(int idClient, int idTrip);
    Task<bool> IsTripFull(int id);
}
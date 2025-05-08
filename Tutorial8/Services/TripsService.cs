using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=apbd;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

    //
    // Wyświetla wszystkie wycieczki biura podróży, wraz z danymi o nich i listą krajów z tabeli Trip
    //
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new Dictionary<int, TripDTO>();

        string command = @"SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName
                        FROM Trip t 
                        JOIN Country_Trip ct ON ct.IdTrip = t.IdTrip 
                        JOIN Country c ON ct.IdCountry = c.IdCountry";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                    TripDTO tripDto;
                    
                    if (!trips.ContainsKey(idTrip))
                    {
                        trips.Add(idTrip, new TripDTO
                        {
                            Id = idTrip,
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            Countries = new List<CountryDTO>()
                        });
                    }

                    Console.Write(idTrip);
                    
                    if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
                    {
                        trips[idTrip].Countries.Add(new CountryDTO
                        {
                            Name = reader.GetString(reader.GetOrdinal("CountryName"))
                        });
                    }
                }
            }
        }
        return trips.Values.ToList();
    }

    //
    // Wyświetla wszystkie wycieczki, na które zarejestrowane jest klient o wskazanym id
    //
    public async Task<List<ClientTripDTO>> GetClientTrips(int id)
    {
        var trips = new Dictionary<int, ClientTripDTO>();

        string command = @"SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, 
                            c.Name AS CountryName,
                            ct.RegisteredAt, ct.PaymentDate
                           FROM Client_Trip ct
                           JOIN Trip t ON ct.IdTrip = t.IdTrip
                           JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
                           JOIN Country c ON ctr.IdCountry = c.IdCountry
                           WHERE ct.IdClient = @ClientId";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@ClientId", id);
            
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                    
                    if (!trips.ContainsKey(idTrip))
                    {
                        trips.Add(idTrip, new ClientTripDTO
                        {
                            Id = idTrip,
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            Countries = new List<CountryDTO>(),
                            RegisteredAt = reader.GetInt32(reader.GetOrdinal("RegisteredAt")),
                            PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? 
                                null : reader.GetInt32(reader.GetOrdinal("PaymentDate")),
                        });
                    }
                    
                    if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
                    {
                        trips[idTrip].Countries.Add(new CountryDTO
                        {
                            Name = reader.GetString(reader.GetOrdinal("CountryName"))
                        });
                    }
                }
            }
        }

        return trips.Values.ToList();
    }

    //
    // Tworzy nowego klienta i wstawia go do tabeli Client
    //
    public async Task<int> CreateClient(ClientDTO client)
    {
        string command = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                            OUTPUT INSERTED.IdClient
                            VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);";
        
        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
            cmd.Parameters.AddWithValue("@LastName", client.LastName);
            cmd.Parameters.AddWithValue("@Email", client.Email);
            cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", client.Pesel);

            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }

    //
    // Dodaje do tabeli Client_Trip nową rejestrację klienta na wycieczkę
    //
    public async Task RegisterClientForTrip(int idClient, int idTrip)
    {
        string command = @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                           VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL)";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", idClient);
            cmd.Parameters.AddWithValue("@IdTrip", idTrip);
            cmd.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
            
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
    
    //
    // Usuwa rejestrację z tabeli Client_Trip danego klienta
    //
    public async Task UnregisterClientFromTrip(int idClient, int idTrip)
    {
        string command = @"DELETE FROM Client_Trip 
                           WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", idClient);
            cmd.Parameters.AddWithValue("@IdTrip", idTrip);
            
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }

    //
    // Sprawdzamy czy istnieje wycieczka o danym id
    //
    public async Task<bool> DoesTripExist(int id)
    {
        string command = @"SELECT 1 FROM Trip WHERE IdTrip = @IdTrip";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@IdTrip", id);
            await conn.OpenAsync();
            
            var result = await cmd.ExecuteScalarAsync();
            
            return result != null;
        }
    }
    
    //
    // Sprawdzamy czy klient posiada wycieczki
    //
    public async Task<bool> DoClientTripsExist(int id)
    {
        string command = @"SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", id);
            await conn.OpenAsync();
            
            var result = await cmd.ExecuteScalarAsync();
            
            return result != null;
        }
    }
    
    //
    // Sprawdzamy czy istnieje klient o danym id
    //
    public async Task<bool> DoesClientExist(int id)
    {
        string command = @"SELECT 1 FROM Client WHERE IdClient = @IdClient";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", id);
            await conn.OpenAsync();
            
            var result = await cmd.ExecuteScalarAsync();
            
            return result != null;
        }
    }
    
    //
    // Sprawdzamy czy istnieje klient o danym numerze pesel
    //
    public async Task<bool> DoesClientPeselExist(string pesel)
    {
        string command = @"SELECT 1 FROM Client WHERE Pesel = @Pesel";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@Pesel", pesel);
            await conn.OpenAsync();
            
            var result = await cmd.ExecuteScalarAsync();
            
            return result != null;
        }
    }

    //
    // Sprawdzamy czy klient zarejestrowany jest na daną wycieczkę
    //
    public async Task<bool> IsClientRegisteredForTrip(int idClient, int idTrip)
    {
        string command = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@IdClient", idClient);
            cmd.Parameters.AddWithValue("@IdTrip", idTrip);
            
            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            
            return result != null;
        }
    }

    //
    // Sprawdzamy czy na wycieczke zapełniono już wszystkie wolne miejsca
    //
    public async Task<bool> IsTripFull(int id)
    {
        string command = @"SELECT CASE WHEN COUNT(ct.IdClient) >= t.MaxPeople THEN 1 ELSE 0 END
                           FROM Trip t
                           LEFT JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
                           WHERE t.IdTrip = @IdTrip
                           GROUP BY t.MaxPeople";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@IdTrip", id);
            await conn.OpenAsync();
            
            var result = await cmd.ExecuteScalarAsync();
            
            return Convert.ToInt32(result) == 1;
        }
    }
}
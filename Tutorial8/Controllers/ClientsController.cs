using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/clients")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public ClientsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }
        
        //
        // GET /api/clients/{id}/trips
        // Wyświetla wszystkie wycieczki, na które zarejestrowane jest klient o wskazanym id,
        // sprawdzamy również czy dany klient istnieje oraz czy posiada zarejstrowane wycieczki
        //
        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            if (!await _tripsService.DoesClientExist(id))
            {
                return NotFound("Nie znaleziono klienta");
            }
            
            if (!await _tripsService.DoClientTripsExist(id))
            {
                return NotFound("Nie znaleziono wycieczek klienta");
            }
            
            var trips = await _tripsService.GetClientTrips(id);
            return Ok(trips);
        }

        //
        // POST /api/clients
        // Tworzy nowego klienta poprzez podanie JSON w body żądania,
        // weryfikujemy poprawność podanego JSON klienta oraz to czy klient o tym samym numerze PESEL istnieje
        //
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] ClientDTO client)
        {
            if (string.IsNullOrEmpty(client.FirstName))
                return BadRequest("Nie podano imienia");

            if (string.IsNullOrEmpty(client.LastName))
                return BadRequest("Nie podano nazwiska");

            if (string.IsNullOrEmpty(client.Email))
                return BadRequest("Nie podano adresu email");

            if (string.IsNullOrEmpty(client.Telephone))
                return BadRequest("Nie podano numeru telefonu");

            if (string.IsNullOrEmpty(client.Pesel))
                return BadRequest("Nie podano numeru PESEL");

            if (client.Pesel.Length != 11)
                return BadRequest("PESEL musi składać się z 11 znaków");

            if (!client.Email.Contains("@"))
                return BadRequest("Nie prawidłowy adres email");

            if (await _tripsService.DoesClientPeselExist(client.Pesel))
            {
                return Conflict("Podany numer PESEL istnieje");
            }
            
            int clientId = await _tripsService.CreateClient(client);
            return CreatedAtAction(nameof(GetClientTrips), new { id = clientId }, new { Id = clientId });
        }

        //
        // PUT /api/clients/{id}/trips/{tripId}
        // Rejestruje klienta o wskazanym id na wycieczkę o wskazanym id,
        // sprawdzamy czy klient i wycieczka istnieją oraz czy nie osiągnieto max klientów na wycieczkę
        //
        [HttpPut("{idClient}/trips/{idTrip}")]
        public async Task<IActionResult> RegisterClientForTrip(int idClient, int idTrip)
        {
            if (!await _tripsService.DoesClientExist(idClient))
            {
                return NotFound("Nie znaleziono klienta");
            }
            if (!await _tripsService.DoesTripExist(idTrip))
            {
                return NotFound("Nie znaleziono wycieczki");
            }
            if (await _tripsService.IsClientRegisteredForTrip(idClient, idTrip))
            {
                return Conflict("Klient jest już zarejestrowany na podaną wycieczkę");
            }

            if (await _tripsService.IsTripFull(idTrip))
            {
                return Conflict("Brak wolnych miejsc na podanej wycieczce");
            }
            
            await _tripsService.RegisterClientForTrip(idClient, idTrip);
            return Ok("Klient zarejestrowany na wycieczkę");
        }
        
        //
        // DELETE /api/clients/{id}/trips/{tripId}
        // Usuwa rejestrację klienta o wskazanym id na wycieczkę o wskazanym id,
        // sprwadzamy czy wycieczka istnieje
        //
        [HttpDelete("{idClient}/trips/{idTrip}")]
        public async Task<IActionResult> UnegisterClientForTrip(int idClient, int idTrip)
        {
            if (!await _tripsService.IsClientRegisteredForTrip(idClient, idTrip))
            {
                return NotFound("Rejestracja nie istnieje");
            }
            
            await _tripsService.UnregisterClientFromTrip(idClient, idTrip);
            return Ok("Usunięto rezerwację");
        }
    }
}
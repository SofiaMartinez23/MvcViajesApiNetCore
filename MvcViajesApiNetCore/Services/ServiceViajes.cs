using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NugetViajesSMG.Models;
using System.Net.Http.Headers;
using System.Text;
using NuGet.Common;
using System.Security.Claims;

namespace MvcViajesApiNetCore.Services
{
    public class ServiceViajes
    {
        private string UrlApi;
        private MediaTypeWithQualityHeaderValue Header;
        private IHttpContextAccessor contextAccessor;

        public ServiceViajes(IConfiguration configuration
            , IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
            this.UrlApi = configuration.GetValue<string>
                ("ApiUrls:ApiViajes");
            this.Header = new
                 MediaTypeWithQualityHeaderValue("application/json");
        }

        public async Task<string> GetTokenAsync
            (string email, string clave)
        {
            using (HttpClient client = new HttpClient())
            {
                string request = "api/auth/login";
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                LoginModel model = new LoginModel
                {
                    Email = email,
                    Clave = clave
                };
                string json = JsonConvert.SerializeObject(model);
                StringContent content = new StringContent
                    (json, Encoding.UTF8, "application/json");
                HttpResponseMessage response =
                    await client.PostAsync(request, content);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content
                        .ReadAsStringAsync();
                    JObject keys = JObject.Parse(data);
                    string token = keys.GetValue("response").ToString();
                    return token;
                }
                else
                {
                    return null;
                }
            }
        }

        private async Task<T> CallApiAsync<T>(string request)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                HttpResponseMessage response =
                    await client.GetAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    T data = await response.Content.ReadAsAsync<T>();
                    return data;
                }
                else
                {
                    return default(T);
                }
            }
        }

        private async Task<T> CallApiAsync<T>
            (string request, string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Add
                    ("Authorization", "bearer " + token);
                HttpResponseMessage response =
                    await client.GetAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    T data = await response.Content.ReadAsAsync<T>();
                    return data;
                }
                else
                {
                    return default(T);
                }
            }
        }

        #region ZONA USUARIOS

        public async Task<List<UsuarioCompletoView>> GetUsuariossAsync()
        {
            string token =
                this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN").Value;
            string request = "api/usuarios";
            List<UsuarioCompletoView> usuarios = await
                this.CallApiAsync<List<UsuarioCompletoView>>(request, token);
            return usuarios;
        }

        public async Task<UsuarioCompletoView> FindUsuarioAsync
            (int idUsuario)
        {
            string token =
                this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN").Value;
            string request = "api/usuarios/" + idUsuario;
            UsuarioCompletoView empleado = await
                this.CallApiAsync<UsuarioCompletoView>(request, token);
            return empleado;
        }

        public async Task<UsuarioCompletoView> GetPerfilAsync()
        {
            string token =
                this.contextAccessor.HttpContext.User
                .FindFirst(x => x.Type == "TOKEN").Value;
            string request = "api/usuarios/perfil";
            UsuarioCompletoView empleado = await
                this.CallApiAsync<UsuarioCompletoView>(request, token);
            return empleado;
        }
        public async Task UpdateUsuarioAsync(
        string nombre, string email, int edad, string nacionalidad,
        string preferenciaViaje, string clave, string confirmarClave,
        string avatarUrl)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    // If not found in claims, fallback to getting from the profile
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/usuarios/updateusuario/" + idUsuario;
                UsuarioCompletoView usuario = new UsuarioCompletoView();
                usuario.IdUsuario = idUsuario; 
                usuario.Nombre = nombre;
                usuario.Email = email;
                usuario.Edad = edad;
                usuario.Nacionalidad = nacionalidad;
                usuario.PreferenciaViaje = preferenciaViaje;
                usuario.Clave = clave;
                usuario.ConfirmarClave = confirmarClave;
                usuario.AvatarUrl = avatarUrl;

                string json = JsonConvert.SerializeObject(usuario);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al updatear usuario: " + response.StatusCode);
                }
            }
        }

        #endregion

        #region ZONA LUGARES
        public async Task<List<Lugar>> GetLugaresAsync()
        {
            string request = "api/lugares";
            List<Lugar> lugar = await
                this.CallApiAsync<List<Lugar>>(request);
            return lugar;
        }

        public async Task<List<Lugar>> GetLugaresPorUsuarioAsync(int idUsuario)
        {
            string token = this.contextAccessor.HttpContext.User
               .FindFirst(z => z.Type == "TOKEN").Value;
            string request = "api/Lugares/lugaresusuario/" + idUsuario;
            List<Lugar> lugar = await
                this.CallApiAsync<List<Lugar>>(request, token);
            return lugar;
        }

        public async Task<Lugar> FindLugarAsync
           (int idLugar)
        {
            string request = "api/lugares/" + idLugar;
            Lugar lugar = await
                this.CallApiAsync<Lugar>(request);
            return lugar;
        }

        public async Task InsertLugarAsync(
         string nombre, string descripcion, string ubicacion,
         string categoria, DateTime horario, string imagen,
         string tipo)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out idUsuario))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    // If not found in claims, fallback to getting from the profile
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        idUsuario = perfil.IdUsuario;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                List<Lugar> allLugares = await GetLugaresAsync();
                int newIdLugar;
                if (allLugares != null && allLugares.Any())
                {
                    newIdLugar = allLugares.Max(l => l.IdLugar) + 1;
                }
                else
                {
                    newIdLugar = 1;
                }

                string request = "api/lugares";
                Lugar lugar = new Lugar();
                lugar.IdLugar = newIdLugar;
                lugar.Nombre = nombre;
                lugar.Descripcion = descripcion;
                lugar.Ubicacion = ubicacion;
                lugar.Categoria = categoria;
                lugar.Horario = horario;
                lugar.Imagen = imagen;
                lugar.Tipo = tipo;
                lugar.IdUsuario = idUsuario;

                string json = JsonConvert.SerializeObject(lugar);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al insertar lugar: " + response.StatusCode);
                }
            }
        }


        public async Task UpdateLugarAsync(
        int idLugar, string nombre, string descripcion, string ubicacion,
        string categoria, DateTime horario, string imagen,
        string tipo, int idUsuario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN").Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int loggedInUserId = -1;

            var userIdClaim = this.contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out loggedInUserId))
            {
                // IdUsuario found in NameIdentifier
            }
            else
            {
                userIdClaim = this.contextAccessor.HttpContext.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out loggedInUserId))
                {
                    // IdUsuario found in "UserId" claim
                }
                else
                {
                    UsuarioCompletoView perfil = await GetPerfilAsync();
                    if (perfil != null && perfil.IdUsuario != 0)
                    {
                        loggedInUserId = perfil.IdUsuario;
                    }
                    else
                    {
                        throw new Exception("No se pudo extraer el IdUsuario del token o del perfil del usuario.");
                    }
                }
            }

            using (HttpClient client = new HttpClient())
            {
                string request = "api/lugares/" + idLugar;
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                Lugar lugar = new Lugar
                {
                    IdLugar = idLugar,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    Ubicacion = ubicacion,
                    Categoria = categoria,
                    Horario = horario,
                    Imagen = imagen,
                    Tipo = tipo,
                    IdUsuario = idUsuario
                };

                string json = JsonConvert.SerializeObject(lugar);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al actualizar lugar: " + response.StatusCode);
                }
            }
        }



        public async Task DeleteLugarAsync(int idLugar)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN").Value;

            using (HttpClient client = new HttpClient())
            {
                string request = "api/lugares/" + idLugar;
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response = await client.DeleteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al eliminar lugar: " + response.StatusCode);
                }
            }
        }

    #endregion

        #region ZONA COMENTARIOS

        public async Task<List<Comentario>> GetComentarioLugarAsync (int idlugar)
        {
            string request = "api/comentarios/" + idlugar;
            List<Comentario> coment = await
                this.CallApiAsync<List<Comentario>>(request);
            return coment;
        }

        #endregion

        #region ZONA FAVORITOS

        public async Task<List<LugarFavorito>> GetFavoritosUsuarioAsync(int idusuario)
        {
            string token =
                this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;
            string request = "api/favoritos/" + idusuario;
            LugarFavorito favorito = await 
                this.CallApiAsync<LugarFavorito>(request, token);

            return new List<LugarFavorito> { favorito };
        }

        #endregion

    }
}

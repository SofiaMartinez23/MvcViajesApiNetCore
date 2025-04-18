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


            string request = "api/usuarios";
            List<UsuarioCompletoView> usuarios = await
                this.CallApiAsync<List<UsuarioCompletoView>>(request);

            if (idUsuario != -1 && usuarios != null) 
            {
                usuarios = usuarios.Where(u => u.IdUsuario != idUsuario).ToList();
            }

            return usuarios;
        }

        public async Task<List<UsuarioCompletoView>> BuscarUsuariosPorNombreAsync(string nombre)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            string request = $"api/Usuarios/BuscarByNombre?nombre={nombre}";
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

        public async Task<int> GetNextUsuarioIdAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);

                string request = "api/usuarios";

                HttpResponseMessage response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<UsuarioCompletoView> usuarios = JsonConvert.DeserializeObject<List<UsuarioCompletoView>>(json);
                    if (usuarios != null && usuarios.Any())
                    {
                        return usuarios.Max(u => u.IdUsuario) + 1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    throw new Exception("Error al obtener la lista de usuarios para generar el próximo ID: " + response.StatusCode);
                }
            }
        }


        public async Task InsertUsuarioAsync(
           string nombre, string email, int edad, string nacionalidad,
           string preferenciaViaje, string clave, string confirmarClave,
           string avatarUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);

                string request = "api/usuarios/insertusuario";

                UsuarioCompletoView usuario = new UsuarioCompletoView();
                usuario.IdUsuario = await GetNextUsuarioIdAsync();
                usuario.Nombre = nombre;
                usuario.Email = email;
                usuario.Edad = edad;
                usuario.Nacionalidad = nacionalidad;
                usuario.PreferenciaViaje = preferenciaViaje;
                usuario.Clave = clave;
                usuario.ConfirmarClave = confirmarClave;
                usuario.AvatarUrl = avatarUrl;
                usuario.FechaRefistro = DateTime.Now;

                string json = JsonConvert.SerializeObject(usuario);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al registrar usuario: " + response.StatusCode);
                }
            }
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
                string request = "api/lugares/updatelugar/" + idLugar;
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
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al actualizar lugar: {response.StatusCode} - {errorContent}"); // Esto imprimirá el contenido del error en la consola
                    throw new Exception("Error al actualizar lugar: " + response.StatusCode);
                }
            }
        }

        public async Task DeleteLugarAsync(int idLugar)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/lugares/" + idLugar;
                HttpResponseMessage response = await client.DeleteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al eliminar lugar: {response.StatusCode}");
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

        public async Task<Comentario> FindComentarioAsync
          (int idComentario)
        {
            string request = "api/comentarios/findcomentarios/" + idComentario;
            Comentario coment = await
                this.CallApiAsync<Comentario>(request);
            return coment;
        }

        public async Task<int> GetNextComentarioIdAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);

                string request = "api/comentarios"; // Asume que tienes un endpoint para obtener todos los comentarios

                HttpResponseMessage response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<Comentario> comentarios = JsonConvert.DeserializeObject<List<Comentario>>(json);
                    if (comentarios != null && comentarios.Any())
                    {
                        return comentarios.Max(c => c.IdComentario) + 1;
                    }
                    else
                    {
                        return 1; // Si no hay comentarios, el primer ID será 1
                    }
                }
                else
                {
                    throw new Exception("Error al obtener la lista de comentarios para generar el próximo ID: " + response.StatusCode);
                }
            }
        }

        public async Task InsertComentarioAsync(int idLugar, string comentario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            int idUsuario = -1;
            string nombreUsuario = string.Empty;

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
                        nombreUsuario = perfil.Nombre; // Assuming Nombre is available in UsuarioCompletoView
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

                string request = "api/comentarios"; 

                Comentario coment = new Comentario
                {
                    IdComentario = await GetNextComentarioIdAsync(), 
                    IdLugar = idLugar,
                    IdUsuario = idUsuario,
                    Comentarios = comentario,
                    FechaComentario = DateTime.Now,
                    NombreUsuario = nombreUsuario
                };

                string json = JsonConvert.SerializeObject(coment);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Error al updatear usuario: " + response.StatusCode);
                }
            }
        }

        public async Task UpdateComentarioAsync(int idComentario, int idLugar, string comentario, string nombreUsuario)
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
                        nombreUsuario = perfil.Nombre;
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

                string request = "api/comentarios/updatecomentarios/" +  idComentario; 

                Comentario coment = new Comentario
                {
                    IdComentario = idComentario,
                    IdLugar = idLugar,
                    IdUsuario = idUsuario,
                    Comentarios = comentario,
                    FechaComentario = DateTime.Now, 
                    NombreUsuario = nombreUsuario
                };

                string json = JsonConvert.SerializeObject(coment);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al actualizar el comentario: {response.StatusCode} - {errorContent}");
                }
            }
        }

        public async Task DeleteComentarioAsync(int idComentario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = $"api/comentarios/{idComentario}"; // Endpoint para eliminar, incluyendo el ID

                HttpResponseMessage response = await client.DeleteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al eliminar el comentario: {response.StatusCode} - {errorContent}");
                }
            }
        }
        #endregion

        #region ZONA FAVORITOS

        public async Task<List<LugarFavorito>> GetFavoritosAsync()
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("Token de usuario no encontrado para obtener todos los favoritos.");
            }

            string request = "api/favoritos"; 
            List<LugarFavorito> favoritos = await
                this.CallApiAsync<List<LugarFavorito>>(request, token); 

            return favoritos;
        }

        public async Task<List<LugarFavorito>> GetFavoritosUsuarioAsync(int idusuario)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            string request = "api/favoritos/" + idusuario;

            List<LugarFavorito> favoritos = await
                this.CallApiAsync<List<LugarFavorito>>(request, token);

            return favoritos;
        }

        public async Task<int> GetNextFavoritoIdAsync()
        {
            // Calls the method to get ALL favorites
            List<LugarFavorito> allFavoritos = await GetFavoritosAsync();

            if (allFavoritos != null && allFavoritos.Any())
            {
                return allFavoritos.Max(f => f.IdFavorito) + 1;
            }
            else
            {
                return 1;
            }
        }

        public async Task InsertFavoritoAsync(int idLugar)
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

            Lugar lugarDetails = await FindLugarAsync(idLugar); 
            if (lugarDetails == null)
            {
                throw new Exception($"No se encontró el lugar con IdLugar {idLugar} para marcar como favorito.");
            }

  
            int newIdFavorito = await GetNextFavoritoIdAsync();

            LugarFavorito favorito = new LugarFavorito
            {
                IdFavorito = newIdFavorito, 
                IdUsuario = idUsuario, 
                IdLugar = idLugar,
                ImagenLugar = lugarDetails.Imagen,
                NombreLugar = lugarDetails.Nombre,
                DescripcionLugar = lugarDetails.Descripcion,
                UbicacionLugar = lugarDetails.Ubicacion,
                TipoLugar = lugarDetails.Tipo,
                FechaDeVisitaLugar = DateTime.Now 
            };

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/favoritos"; 
                string json = JsonConvert.SerializeObject(favorito);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(request, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error al insertar favorito: {response.StatusCode} - {errorContent}");
                }

            }
        }
        public async Task DeleteFavoritosAsync(int idUsuario, int idLugar)
        {
            string token = this.contextAccessor.HttpContext.User
                .FindFirst(z => z.Type == "TOKEN")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se encontró el token de usuario.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string request = "api/favoritos/" + idUsuario + "/" + idLugar;
                HttpResponseMessage response = await client.DeleteAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al eliminar lugar: {response.StatusCode}");
                }
            }
        }


        #endregion

        #region ZONA SEGUIDORES


        #endregion

    }
}

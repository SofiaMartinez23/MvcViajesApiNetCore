using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NugetViajesSMG.Models;
using System.Net.Http.Headers;
using System.Text;
using NuGet.Common;

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
        public async Task UpdateUsuarioAsync
            (int idUsuario)
        {
            string request = "api/usuarios/updateusuario/"
                + idUsuario;
            
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(this.UrlApi);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(this.Header);
                HttpResponseMessage response =
                    await client.PutAsync(request + "?" , null);
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

        public async Task<Lugar> FindLugarAsync
           (int idLugar)
        {           
            string request = "api/lugares/" + idLugar;
            Lugar lugar = await
                this.CallApiAsync<Lugar>(request);
            return lugar;
        }

        #endregion
    }
}

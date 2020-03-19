using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MarvelAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MarvelAPI.Controllers
{
    public class MarvelController : Controller
    {
        [HttpGet]
        public IActionResult Index(int page)
        {
            try
            {
                return View(GetHeroes(page));
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        private List<Hero> GetHeroes(int page)
        {
            int pageSize = 15;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = client.GetAsync(GenerateApiString(page, pageSize)).Result;
                response.EnsureSuccessStatusCode();

                var responseJson = response.Content.ReadAsStringAsync().Result;
                dynamic result = JsonConvert.DeserializeObject(responseJson);
                Pagination(page, Convert.ToInt32(result.data.total), pageSize);

                return LisHeroes(pageSize, result);
            }
        }

        private List<Hero> LisHeroes(int pageSize, dynamic result)
        {
            var herois = new List<Hero>();
            var urlImgNull = "http://i.annihil.us/u/prod/marvel/i/mg/b/40/image_not_available.jpg";
            for (var i = 0; i != pageSize; i++)
            {
                Hero hero = new Hero
                {
                    ID = result.data.results[i].id,
                    Name = result.data.results[i].name,
                    Description = result.data.results[i].description,
                    ImageUrl = result.data.results[i].thumbnail.path + "." +
                                       result.data.results[i].thumbnail.extension,
                    WikiUrl = IsNullUrl(result.data.results[i].urls[0].url)
                };

                IsNullImage(herois, urlImgNull, hero);
            }

            return herois;
        }

        private static void IsNullImage(List<Hero> herois, string urlImgNull, Hero hero)
        {
            if (!urlImgNull.Equals(hero.ImageUrl))
            {
                herois.Add(hero);
            }
        }

        private void Pagination(int page, int totalHeroes, int pageSize)
        {
            ViewBag.Paging = new Pagination
            {
                TotalPages = (totalHeroes / pageSize) - 1,
                CurrentPage = page
            };
        }

        public string GenerateApiString(int page, int pageSize)
        {
            var timeStamp = DateTime.Now.Ticks.ToString();
            var orderBy = "name";
            var pagination = (Convert.ToInt16(page) * pageSize);
            var publicKey = "3f0440f49c43779bb3901d4e204fae8b";
            var privateKey = "c4452e7e1b880debb82388fe717973fd18f50f8d";
            var hash = GetHash(timeStamp, publicKey, privateKey);
            //var id = "ID HERO";
            //var name = Uri.EscapeUriString("NAME HERO");

            var responseString = "http://gateway.marvel.com/v1/public/characters?" +
                $"orderBy={orderBy}" +
                $"&limit={pageSize}" +
                $"&offset={pagination}" +
                $"&ts={timeStamp}" +
                $"&apikey={publicKey}" +
                $"&hash={hash}";
                //$"&id={id}" +
                //$"name={name}";

            return responseString;
        }

        private string GetHash(string ts, string publicKey, string privateKey)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(ts + privateKey + publicKey);
            var gerador = MD5.Create();
            byte[] bytesHash = gerador.ComputeHash(bytes);

            return BitConverter.ToString(bytesHash).ToLower().Replace("-", String.Empty);
        }

        public string IsNullUrl(dynamic resultado)
        {
            return resultado.Equals(null) ? "https://www.marvel.com/" : (string)resultado;
        }

    }
}
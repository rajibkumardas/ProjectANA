﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using ANAConversationSimulator.Models;

namespace ANAConversationSimulator.Helpers
{
	public class APIHelper
	{
		public static async Task<string> HitAsync(string api)
		{
			using (var client = new HttpClient())
			{
				return await client.GetStringAsync(api);
			}
		}

		public static async Task<T> HitAsync<T>(string api)
		{
			using (var client = new HttpClient())
			{
				var resp = await client.GetStringAsync(api);
				return JsonConvert.DeserializeObject<T>(resp);
			}
		}

		public static async Task<string> HitPostAsync(string api, string data)
		{
			using (var client = new HttpClient())
			{
				var resp = await client.PostAsync(api, new StringContent(data));
				return await resp.Content.ReadAsStringAsync();
			}
		}

		public static async Task<TResponse> HitPostAsync<TRequest, TResponse>(string api, TRequest data)
		{
			using (var client = new HttpClient())
			{
				var resp = await client.PostAsync(api, new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
				resp = resp.EnsureSuccessStatusCode();
				return JsonConvert.DeserializeObject<TResponse>(await resp.Content.ReadAsStringAsync());
			}
		}
		public static async Task HitPostAsync<TRequest>(string api, TRequest data)
		{
			using (var client = new HttpClient())
			{
				var resp = await client.PostAsync(api, new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
				resp = resp.EnsureSuccessStatusCode();
			}
		}

		public static async Task<UploadFileResponse> UploadFile(string fileName, StorageFile sFile)
		{
			using (var client = new HttpClient())
			{
				using (var fStrm = await sFile.OpenStreamForReadAsync())
				{
					Utils.APISettings.Values.TryGetValue("UploadFileAPI", out object UploadFileAPI);
					if (string.IsNullOrWhiteSpace(UploadFileAPI + ""))
					{
						Utils.ShowDialog("Upload File API is not set. Please go to Menu(...) -> Settings and set it.");
						return null;
					}
					var postData = new MultipartFormDataContent();
					postData.Add(new StreamContent(fStrm), "file", sFile.Name);

					var resp = await client.PostAsync(UploadFileAPI + "", postData);
					resp = resp.EnsureSuccessStatusCode();
					var respJson = await resp.Content.ReadAsStringAsync();
					return JsonConvert.DeserializeObject<UploadFileResponse>(respJson);
				}
			}
		}

		public static async Task<(string City, string Country, string Pincode, string FormattedAddress)> LoadCityCountryFromLatLongAsync(double lat, double lng)
		{
			try
			{
				using (var hc = new HttpClient())
				{
					var resp = await hc.GetStringAsync($"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&sensor=true");
					var jobjResp = JObject.Parse(resp);
					if (jobjResp["status"].ToString() == "OK")
					{
						var result = jobjResp["results"].First;
						var addressComponents = result["address_components"];
						var formattedAddress = result["formatted_address"] + "";
						var city = addressComponents.FirstOrDefault(x => { var types = x["types"].Select(y => y.ToString()); return types.Contains("locality") || types.Contains("administrative_area_level_2"); });
						var country = addressComponents.FirstOrDefault(x => { var types = x["types"].Select(y => y.ToString()); return types.Contains("country"); });
						var pincode = addressComponents.FirstOrDefault(x => { var types = x["types"].Select(y => y.ToString()); return types.Contains("postal_code"); });
						//
						return (City: city?["long_name"] + "", Country: country?["long_name"] + "", pincode?["long_name"] + "", FormattedAddress: formattedAddress);
					}
				}
			}
			catch (Exception ex)
			{
				Utils.ShowDialog(ex.Message);
			}
			return (null, null, null, null);
		}
	}
}

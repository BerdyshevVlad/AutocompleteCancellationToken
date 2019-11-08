using AutocompleteCancellationToken.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AutocompleteCancellationToken.Controllers
{
        // run app with command 'dotnet run' to see logs
        [Produces("application/json")]
        [Route("api/sample")]
        public class SampleController : Controller
        {
            private readonly HttpClient _httpClient;
            private readonly ILogger<SampleController> _logger;

            public SampleController(HttpClient httpClient, ILogger<SampleController> logger)
            {
                _httpClient = httpClient;
                _logger = logger;
            }

            [Route("thing")]
            public async Task<IActionResult> GetAThingAsync(CancellationToken ct)
            {
                try
                {
                    await _httpClient.GetAsync("http://httpstat.us/204?sleep=5000", ct);
                    _logger.LogInformation("Task completed!");
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Task canceled!");

                }
                return NoContent();
            }

            [Route("anotherthing")]
            public async Task<IActionResult> GetAnotherThingAsync(CancellationToken ct)
            {
                try
                {
                    for (var i = 0; i < 5; ++i)
                    {
                        ct.ThrowIfCancellationRequested();
                        //do stuff...
                        await Task.Delay(1000, ct);
                    }
                    _logger.LogInformation("Process completed!");
                }
                catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    _logger.LogInformation("Process canceled!");

                }
                return NoContent();
            }

            [HttpGet]
            [Route("phoneSearch/{searchCriteria}")]
            public async Task<IActionResult> UserSearch(string searchCriteria)
            {
                _logger.LogInformation("Searching for {0}....", searchCriteria);
                var result = new List<string>();
                try
                {

                    if (AppConsts.cancellationTokenSource != null &&
                       !AppConsts.cancellationTokenSource.IsCancellationRequested)
                    {
                        AppConsts.cancellationTokenSource.Cancel();
                    }

                    AppConsts.cancellationTokenSource = new CancellationTokenSource();

                    result = await SearchAsync(searchCriteria, AppConsts.cancellationTokenSource.Token);

                    AppConsts.cancellationTokenSource = null;

                    _logger.LogInformation("Search({0}) returned {1} records", searchCriteria, result.Count);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Search({0}) cancelled", searchCriteria);
                }

                return Ok(result);
            }


            private async Task<List<string>> SearchAsync(string searchCriteria, CancellationToken cancelToken)
            {
                List<string> results = await GetFromDB(searchCriteria, cancelToken);

                _logger.LogInformation("Search({0}) returned {1} records", searchCriteria, results.Count);

                return results;
            }

            private async Task<List<string>> GetFromDB(string searchCriteria, CancellationToken ct)
            {
                var connectionString = "Data Source=localhost;Initial Catalog=mobilestoredb;Integrated Security=True;";
                SqlConnection con = new SqlConnection(connectionString);
                SqlCommand cmd = new SqlCommand();

                cmd.CommandText = $" Select Name From [Products] Where Name Like '{searchCriteria}%'";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = con;

                await Task.Delay(5000);

                await con.OpenAsync(ct);

                var result = new List<string>();

                using (SqlDataReader rdr = await cmd.ExecuteReaderAsync(ct))
                {
                    while (await rdr.ReadAsync(ct))
                    {
                        var myString = rdr.GetString(0); //The 0 stands for "the 0'th column", so the first column of the result.
                                                         // Do somthing with this rows string, for example to put them in to a list
                        result.Add(myString);
                    }
                }

                con.Close();

                return result;

            }
        }
    }
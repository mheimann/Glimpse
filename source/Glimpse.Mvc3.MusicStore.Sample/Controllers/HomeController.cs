using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using Dapper;
using MvcMusicStore.Models;
using Glimpse.Ado.AlternateType;
using System.Reflection;

namespace MvcMusicStore.Controllers
{
    public static class DbCommandExtensions
    {
        public static DbCommand AddParameter(this DbCommand source, string parameterName, string sourceColumn, DbType parameterType, DataRowVersion sourceVersion = DataRowVersion.Default, int fieldSize = 0)
        {
            DbParameter parameter = source.CreateParameter();

            parameter.ParameterName = parameterName;
            parameter.DbType = parameterType;
            parameter.SourceColumn = sourceColumn;
            parameter.SourceVersion = sourceVersion;

            if (fieldSize > 0)
                parameter.Size = fieldSize;

            source.Parameters.Add(parameter);

            return source; // Allow chaining
        }
    }

    public class HomeController : Controller
    {
        //
        // GET: /Home/

        MusicStoreEntities storeDB = new MusicStoreEntities();

        public ActionResult Index()
        {
            // Get most popular albums
            var albums = GetTopSellingAlbums(5);
            var albumCount = GetTotalAlbumns();

            Trace.Write(string.Format("Total number of Albums = {0} and Albums with 'The' = {1}", albumCount.Item1, albumCount.Item2));
            Trace.Write("Got top 5 albums");
            Trace.TraceWarning("Test TraceWarning;");
            Trace.IndentLevel++;
            Trace.TraceError("Test TraceError;");
            Trace.Write("Another trace line");
            Trace.IndentLevel++;
            Trace.Write("Yet another trace line");
            Trace.IndentLevel = 0;
            Trace.TraceInformation("Test TraceInformation;");

            HttpContext.Session["TestObject"] = new Artist { ArtistId = 123, Name = "Test Artist" };

            TraceSource ts = new TraceSource("Test source");

            ts.TraceEvent(TraceEventType.Warning, 0, string.Format("{0}: {1}", "trace", "source"));

            GetAllAlbums();
            UpdateSomeAlbums();

            return View(albums);
        }

        private void UpdateSomeAlbums()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MusicStoreEntities"];
            var factory = DbProviderFactories.GetFactory(connectionString.ProviderName);

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString.ConnectionString;
                connection.Open();

                using (var tx = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM Albums";

                        DbDataAdapter dbAdapter = factory.CreateDataAdapter();
                        dbAdapter.SelectCommand = cmd;

                        // Build modification queries
                        DbCommandBuilder cmdBuilder = factory.CreateCommandBuilder();

                        #region This is a total MacGyver HACK
                        PropertyInfo innerDataAdapterProperty = typeof(GlimpseDbDataAdapter).GetProperty("InnerDataAdapter", BindingFlags.NonPublic | BindingFlags.Instance);
                        var innerAdapter = (DbDataAdapter)innerDataAdapterProperty.GetValue(dbAdapter, null);
                        innerAdapter.SelectCommand = cmd;
                        #endregion

                        cmdBuilder.DataAdapter = innerAdapter;

                        dbAdapter.InsertCommand = cmdBuilder.GetInsertCommand(true);
                        // The command builder creates an invalid queries for some reason!?
                        //dbAdapter.DeleteCommand = cmdBuilder.GetDeleteCommand(true);
                        //dbAdapter.UpdateCommand = cmdBuilder.GetUpdateCommand(true);

                        var updateCommand = connection.CreateCommand();
                        updateCommand.CommandText =
                            @"UPDATE [Albums] SET [GenreId] = @GenreId, [ArtistId] = @ArtistId, [Title] = @Title, [Price] = @Price, [AlbumArtUrl] = @AlbumArtUrl WHERE ([AlbumId] = @AlbumId)";

                        updateCommand
                            .AddParameter("@GenreId", "GenreId", DbType.Int32)
                            .AddParameter("@ArtistId", "ArtistId", DbType.Int32)
                            .AddParameter("@Title", "Title", DbType.String, fieldSize: 160)
                            .AddParameter("@Price", "Price", DbType.Decimal, fieldSize: 9)
                            .AddParameter("@AlbumArtUrl", "AlbumArtUrl", DbType.String, fieldSize: 1024)
                            .AddParameter("@AlbumId", "AlbumId", DbType.Int32, sourceVersion: DataRowVersion.Original);

                        dbAdapter.UpdateCommand = updateCommand;

                        var deleteCommand = connection.CreateCommand();
                        deleteCommand.CommandText = @"DELETE FROM [Albums] WHERE [AlbumId] = @AlbumId";

                        deleteCommand
                            .AddParameter("@AlbumId", "AlbumId", DbType.Int32, sourceVersion: DataRowVersion.Original);

                        dbAdapter.DeleteCommand = deleteCommand;

                        DataTable dataTable = new DataTable();
                        dbAdapter.Fill(dataTable);

                        #region Insert
                        DataRow newAlbum = dataTable.NewRow();
                        newAlbum["GenreId"] = 7; // Metal
                        newAlbum["ArtistId"] = 21; // Black Sabbath
                        newAlbum["Title"] = "The fictional album";
                        newAlbum["Price"] = 8.99;
                        newAlbum["AlbumArtUrl"] = "/Content/Images/placeholder.gif";
                        dataTable.Rows.Add(newAlbum);

                        dbAdapter.Update(dataTable);
                        #endregion

                        #region Update
                        DataRow[] updateRows = dataTable.Select("ArtistId = 20");
                        foreach (var album in updateRows)
                        {
                            album["Price"] = 6.99;
                        }

                        dbAdapter.Update(dataTable);                        
                        #endregion

                        #region Delete
                        // Delete all "The Doors" albums (ArtistId 120) because they're sold out *cough*
                        DataRow[] deleteRows = dataTable.Select("ArtistId = 120");
                        foreach (var album in deleteRows)
                        {
                            album.Delete();
                        }

                        dbAdapter.Update(dataTable);
                        #endregion
                    }

                    tx.Rollback(); // Rollback everything since we don't want to really change any data
                }
            }
        }

        [NoCache]
        public virtual ActionResult News()
        {
            var views = new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight" };

            var randomIndex = new Random().Next(0, views.Count());

            Trace.Write("Randomly selected story number " + randomIndex);

            return PartialView(views[randomIndex]);
        }

        private List<Album> GetTopSellingAlbums(int count)
        {
            // Group the order details by album and return
            // the albums with the highest count

            return storeDB.Albums
                .OrderByDescending(a => a.OrderDetails.Count())
                .Take(count)
                .ToList();
        }

        private int GetAllAlbums()
        {  
            var rowcount = 0; 
            var ds = new DataSet();

            var connectionString = ConfigurationManager.ConnectionStrings["MusicStoreEntities"];
            var factory = DbProviderFactories.GetFactory(connectionString.ProviderName);  //factory: {Glimpse.Ado.AlternateType.GlimpseDbProviderFactory<System.Data.SqlClient.SqlClientFactory>}

            using (DbCommand cmd = factory.CreateCommand()) //cmd: {Glimpse.Ado.AlternateType.GlimpseDbCommand} 
            { 
                //cmd.CommandType = CommandType.StoredProcedure; 
                cmd.CommandText = "SELECT * FROM Albums";

                using (DbConnection con = factory.CreateConnection()) //con: {Glimpse.Ado.AlternateType.GlimpseDbConnection} 
                {
                    con.ConnectionString = connectionString.ConnectionString; 
                    cmd.Connection = con;

                    IDbDataAdapter dbAdapter = factory.CreateDataAdapter(); //dbAdapter: {System.Data.SqlClient.SqlDataAdapter} not GlimpseDbDataAdapter 
                    dbAdapter.SelectCommand = cmd;

                    dbAdapter.Fill(ds);
                } 
            }

            rowcount = ds.Tables[0].Rows.Count;

            return rowcount; 
        }

        private Tuple<int, int> GetTotalAlbumns()
        {
            var result1 = 0;
            var result2 = 0;
            var result3 = 0;

            var connectionString = ConfigurationManager.ConnectionStrings["MusicStoreEntities"];
            var factory = DbProviderFactories.GetFactory(connectionString.ProviderName); 
            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString.ConnectionString;
                connection.Open();

                using (var command = connection.CreateCommand())
                { 
                    command.CommandText = "SELECT COUNT(*) FROM Albums WHERE Title LIKE 'A%'";
                    command.CommandType = CommandType.Text;
                    result3 = (int)command.ExecuteScalar();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Albums WHERE Title LIKE 'B%'";
                    command.CommandType = CommandType.Text;
                    result3 = (int)command.ExecuteScalar();
                }

                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;

                        command.CommandText = "SELECT COUNT(*) FROM Albums";
                        command.CommandType = CommandType.Text;
                        result1 = (int)command.ExecuteScalar();
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;

                        command.CommandText = "SELECT COUNT(*) FROM Albums WHERE Title LIKE 'C%'";
                        command.CommandType = CommandType.Text;
                        result2 = (int)command.ExecuteScalar();
                    }

                    transaction.Commit();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Albums WHERE Title LIKE 'D%'";
                    command.CommandType = CommandType.Text;
                    result3 = (int)command.ExecuteScalar();
                }
                    
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;

                        command.CommandText = "SELECT COUNT(*) FROM Albums WHERE Title LIKE 'E%'";
                        command.CommandType = CommandType.Text;
                        result3 = (int)command.ExecuteScalar();
                    } 
                    //transaction.Commit();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Albums WHERE Title LIKE 'F%'";
                    command.CommandType = CommandType.Text;
                    result3 = (int)command.ExecuteScalar();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Albums WHERE Title LIKE 'G%'";
                    command.CommandType = CommandType.Text;
                    result3 = (int)command.ExecuteScalar();
                }
            }

            using (var connection = factory.CreateConnection())
            {  
                connection.ConnectionString = connectionString.ConnectionString;
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Albums WHERE Title LIKE 'I%'";
                    command.CommandType = CommandType.Text;
                    var result = command.ExecuteReader();
                }

                using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Albums WHERE Title LIKE 'J%'";
                        command.CommandType = CommandType.Text;
                        var result = command.ExecuteReader();
                    }

                    scope.Complete();
                }

                var albums = connection.Query<Album>("SELECT * FROM Albums WHERE Title LIKE 'K%'");
            }

            var test = storeDB.Database.ExecuteSqlCommand("SELECT count(*) FROM Albums WHERE Title LIKE 'The%'");


            return new Tuple<int, int>(result1, result2);
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public sealed class NoCacheAttribute : ActionFilterAttribute
        {
            public override void OnResultExecuting(ResultExecutingContext filterContext)
            {
                filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
                filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
                filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
                filterContext.HttpContext.Response.Cache.SetNoStore();
                 
                base.OnResultExecuting(filterContext);
            }
        }
    }
}

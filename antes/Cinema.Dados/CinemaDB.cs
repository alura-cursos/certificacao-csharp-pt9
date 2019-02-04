using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cinema.Dados
{
    public class CinemaDB
    {
        private readonly string databaseServer;
        private readonly string masterDatabase;
        private readonly string databaseName;

        public CinemaDB(string databaseServer, string masterDatabase, string databaseName)
        {
            this.databaseServer = databaseServer;
            this.masterDatabase = masterDatabase;
            this.databaseName = databaseName;
        }

        public async Task CriarBancoDeDadosAsync()
        {
            await CriarBancoAsync();
            await CriarTabelasAsync();
            await InserirRegistrosAsync();
        }

        private async Task CriarBancoAsync()
        {
            string sql = $@"IF EXISTS (SELECT * FROM sys.databases WHERE name = N'{databaseName}')
                    BEGIN
                        DROP DATABASE [{databaseName}]
                    END;
                    CREATE DATABASE [{databaseName}];";
            await ExecutarComandoAsync(sql, masterDatabase);
        }

        private async Task CriarTabelasAsync()
        {
            string sql = $@"CREATE TABLE [dbo].[Diretores] (
                        [Id]   INT           IDENTITY (1, 1) NOT NULL,
                        [Nome] VARCHAR (255) NOT NULL
                    );
                    CREATE TABLE [dbo].[Filmes] (
                        [Id]        INT           IDENTITY (1, 1) NOT NULL,
                        [DiretorId] INT           NOT NULL,
                        [Titulo]    VARCHAR (255) NOT NULL,
                        [Ano]       INT           NOT NULL,
                        [Minutos]   INT           NOT NULL
                    );";
            await ExecutarComandoAsync(sql, databaseName);
        }

        private async Task InserirRegistrosAsync()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var sql = new StringBuilder();

            using (Stream stream = assembly.GetManifestResourceStream("Cinema.Dados.Diretores.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    sql.AppendLine($"INSERT Diretores (Nome) VALUES ('{line}');");
                }
            }

            using (Stream stream = assembly.GetManifestResourceStream("Cinema.Dados.Filmes.txt"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    string[] fields = line.Split(',');
                    string diretorId = fields[0];
                    string titulo = fields[1];
                    string ano = fields[2];
                    string minutos = fields[3];
                    sql.AppendLine($"INSERT Filmes (DiretorId, Titulo, Ano, Minutos) VALUES ({diretorId},'{titulo}',{ano},{minutos})");
                }
            }

            await ExecutarComandoAsync(sql.ToString(), databaseName);
        }

        private async Task ExecutarComandoAsync(string sql, string banco)
        {
            SqlConnection conexao = new SqlConnection($"Server={databaseServer};Integrated security=SSPI;database={banco}");
            SqlCommand comando = new SqlCommand(sql, conexao);
            try
            {
                conexao.Open();
                await comando.ExecuteNonQueryAsync();
                Console.WriteLine("Script executado com sucesso.");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (conexao.State == ConnectionState.Open)
                {
                    conexao.Close();
                }
            }
        }

        public async Task<IList<Filme>> GetFilmes()
        {
            IList<Filme> filmes = new List<Filme>();
            string connectionString = $"Server={databaseServer};Integrated security=SSPI;database={databaseName}";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(
                    " SELECT d.Nome AS Diretor, f.Titulo AS Titulo" +
                    " FROM Filmes AS f" +
                    " INNER JOIN Diretores AS d" +
                    "   ON d.Id = f.DiretorId"
                    , connection);
                SqlDataReader reader = await command.ExecuteReaderAsync();

                while (reader.Read())
                {
                    string diretor = reader["Diretor"].ToString();
                    string titulo = reader["Titulo"].ToString();
                    filmes.Add(new Filme(diretor, titulo));
                }
            }

            return filmes;
        }
    }
}

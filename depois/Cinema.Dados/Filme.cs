namespace Cinema.Dados
{
    public class Filme
    {
        public Filme(string diretor, string titulo)
        {
            Diretor = diretor;
            Titulo = titulo;
        }

        public string Diretor { get; set; }
        public string Titulo { get; set; }
    }
}

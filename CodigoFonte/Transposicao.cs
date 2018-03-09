﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Enxadrista
{
    /// <summary>
    /// Tabela de Transposição.
    /// </summary>
    /// <remarks>
    /// Esta tabela será usada para armazenar informações sobre as posições visitadas, e 
    /// quando a mesma posição for visitada novamente, as informações armazenadas podem 
    /// ser usadas para economizar tempo.
    /// Normalmente, o usuário irá decidir o tamanho da tabela e pode ser muito grande, 
    /// desde que tenha memória disponível. Este é um componente importante para programas 
    /// competitivos.
    /// Enxadrista usa uma tabela muito pequena, mas isso deve dar uma boa idéia de como 
    /// funciona.
    /// A tabela terá várias entradas dependendo da memória disponível. Normalmente, o 
    /// número de entradas é definido quando o programa é iniciado com base no tamanho da tabela.
    /// Cada entrada terá quatro registros, onde podemos armazenar informações sobre a posição.
    /// Cada registro terá informações sobre uma posição diferente.
    /// Esta estrutura pode variar de programa para programa.
    /// A posição é identificada pela chave zobrist, você pode ver como ela é calculada na
    /// classe zobrist.cs.
    /// Vamos armazenar no registro, a profundidade, o valor da posição, o melhor movimento
    /// e um valor para indicar qual o tipo de valor que temos. Veja a classe Registro para
    /// mais detalhes sobre estes dados.
    /// 
    /// É pouco difícil implementar a tabela de transposição. Algo que pode ajudar são as 
    /// seguintes posições, onde é muito difícil para o programa encontrar o melhor movimento 
    /// sem a implementação correta. Então, use a posição e execute seu programa, 
    /// se não encontrar a melhor jogada de forma consistente com algum tempo de procura,
    /// você pode ter um problema com sua implementação. Eu sempre uso uns 20 segundos 
    /// para a procura.
    /// 
    ///     Posição                                     Melhor movimento
    ///     8/k/3p4/p2P1p2/P2P1P2/8/8/K7 w - - 0 1      a1b1
    ///     2k5/8/1pP1K3/1P6/8/8/8/8 w - -              c6c7
    /// 
    /// </remarks>
    /// <see cref="Transposicao.Registro"/>
    /// <see cref="Zobrist"/>
    public class Transposicao
    {
        /// <summary>
        /// Registro da Tabela de Transposição.
        /// </summary>
        /// <remarks>
        /// Podemos ter até quatro registros em cada entrada.
        /// Cada registro possui informações sobre uma posição.
        /// </remarks>
        public class Registro
        {
            /// <summary>
            /// Chave Zobrist exclusiva para a posição.
            /// </summary>
            public ulong Chave;

            /// <summary>
            /// Profundidade onde o valor da posição foi encontrado.
            /// </summary>
            public int Profundidade;

            /// <summary>
            /// Valor da posição.
            /// </summary>
            public int Valor;

            /// <summary>
            /// Melhor movimento para a posição.
            /// </summary>
            public Movimento Movimento;

            /// <summary>
            /// Número que indica a idade do registro. Usado para substituir registros antigos.
            /// </summary>
            public byte Geracao;

            /// <summary>
            /// Tipo do registro: superior, inferior ou exato.
            /// </summary>
            public byte Tipo;

            /// <summary>
            /// Inicializa o registro.
            /// </summary>
            public void Inicializa()
            {
                Chave = 0;
                Profundidade = 0;
                Valor = 0;
                Movimento = null;
                Geracao = 0;
                Tipo = 0;
            }

            /// <summary>
            /// Indica se o valor no registro pode ser usado na pesquisa.
            /// </summary>
            /// <remarks>
            /// Mesmo quando você encontra o registro na tabela de transposição, você ainda 
            /// precisa verificar se você pode usar o valor da tabela.
            /// 
            /// Quando o valor é armazenado na tabela, também armazenamos o tipo do valor:
            /// - Tipo Exato: Indica que o valor foi resultado de uma busca completa de 
            ///   todos os movimentos na posição, ou seja, o valor estava entre alfa e beta 
            ///   daquela pesquisa.
            /// - Tipo Superior: indica que o valor é o valor máximo para a posição, e foi inferior 
            ///   a Alfa no momento em que foi armazenado.
            /// - Tipo Inferior: indica que o valor estava acima do beta, o que significa que talvez 
            ///   não tenha pesquisado todos os movimentos, porque este valor causou o corte beta.
            /// Olhe na Pesquisa, na função AlfaBeta para ver como os valores são armazenados.
            /// 
            /// 
            /// 
            /// </remarks>
            /// <param name="alfa"></param>
            /// <param name="beta"></param>
            /// <returns></returns>
            public bool PodeUsarValor(int alfa, int beta)
            {
                if (Tipo == Transposicao.REGISTRO_SUPERIOR && Valor <= alfa) return true;
                if (Tipo == Transposicao.REGISTRO_INFERIOR && Valor >= beta) return true;
                if (Tipo == Transposicao.REGISTRO_EXATO && Valor <= alfa) return true;
                if (Tipo == Transposicao.REGISTRO_EXATO && Valor >= beta) return true;
                return false;
            }
        }

        public const int NUMERO_ENTRADAS = 500000;
        public const int NUMERO_REGISTROS = 4;

        public const byte REGISTRO_SUPERIOR = 1;
        public const byte REGISTRO_INFERIOR = 2;
        public const byte REGISTRO_EXATO = 3;

        public Registro[][] Tabela = new Registro[NUMERO_ENTRADAS][];

        private byte Geracao;

        public Transposicao()
        {
            for (int indice_entrada = 0; indice_entrada < NUMERO_ENTRADAS; indice_entrada++) {
                Tabela[indice_entrada] = new Registro[NUMERO_REGISTROS];
                for (int indice_registro = 0; indice_registro < NUMERO_REGISTROS; indice_registro++) {
                    Tabela[indice_entrada][indice_registro] = new Registro();
                }
            }
            Inicializa();
        }

        public void Inicializa()
        {
            Geracao = 0;
            for (int indice_entrada = 0; indice_entrada < NUMERO_ENTRADAS; indice_entrada++) {
                for (int indice_registro = 0; indice_registro < NUMERO_REGISTROS; indice_registro++) {
                    Tabela[indice_entrada][indice_registro].Inicializa();
                }
            }
        }

        public void IncrementaGeracao()
        {
            Geracao++;
        }

        public Registro Recupera(ulong chave, int profundidade)
        {
            Debug.Assert(profundidade >= 0 && profundidade <= Defs.PROFUNDIDADE_MAXIMA);

            int indice_entrada = (int)(chave % NUMERO_ENTRADAS);

            Debug.Assert(indice_entrada >= 0 && indice_entrada < Transposicao.NUMERO_ENTRADAS);

            var entrada = Tabela[indice_entrada];

            var registro = entrada.Where(r => r.Chave == chave && r.Profundidade >= profundidade).FirstOrDefault();
            if (registro != null) registro.Geracao = Geracao;
            return registro;
        }

        public void Salva(ulong chave, int profundidade, int valor, int nivel, byte tipo, Movimento melhor)
        {
            Debug.Assert(profundidade >= 0 && profundidade <= Defs.PROFUNDIDADE_MAXIMA);
            Debug.Assert(valor >= Defs.VALOR_MINIMO && valor <= Defs.VALOR_MAXIMO);
            Debug.Assert(nivel >= 0 && nivel < Defs.NIVEL_MAXIMO);
            Debug.Assert(tipo == REGISTRO_INFERIOR || tipo == REGISTRO_EXATO || tipo == REGISTRO_SUPERIOR);

            int indice_entrada = (int)(chave % NUMERO_ENTRADAS);
            Debug.Assert(indice_entrada >= 0 && indice_entrada < Transposicao.NUMERO_ENTRADAS);

            var entrada = Tabela[indice_entrada];

            var registro = entrada.FirstOrDefault(r => r.Chave == chave);
            if (registro == null) registro = entrada.OrderBy(r => r.Geracao).ThenBy(r => r.Profundidade).First();

            registro.Chave = chave;
            registro.Geracao = Geracao;
            registro.Profundidade = profundidade;
            registro.Tipo = tipo;
            registro.Movimento = melhor != null ? melhor : registro.Movimento;
            registro.Valor = Transposicao.AjustaValorParaTabela(valor, nivel);
        }

        public static int AjustaValorParaTabela(int valor, int nivel)
        {
            if (valor > Defs.AVALIACAO_MAXIMA) return valor + nivel;
            if (valor < Defs.AVALIACAO_MINIMA) return valor - nivel;
            return valor;
        }

        public static int AjustaValorParaProcura(int valor, int nivel)
        {
            if (valor > Defs.AVALIACAO_MAXIMA) return valor - nivel;
            if (valor < Defs.AVALIACAO_MINIMA) return valor + nivel;
            return valor;
        }

    }
}

using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;

namespace OpenAC.Net.Etq.Interpreter.Resolver;

/// <summary>Transforma um comando em bytes do protocolo de impressão.</summary>
public interface ICommandResolver
{
    byte[] Resolve(IPrintCommand command, EtqConfig configuracao);
}
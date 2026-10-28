using System;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;

namespace OpenAC.Net.Etq.Interpreter.Resolver;

/// <summary>Resolvedor fortemente tipado de comandos.</summary>
public sealed class CommandResolver<TCommand>(Func<TCommand, EtqConfig, byte[]> resolver) : ICommandResolver
    where TCommand : IPrintCommand
{
    private readonly Func<TCommand, EtqConfig, byte[]> resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

    public byte[] Resolve(IPrintCommand command, EtqConfig configuracao) => resolver((TCommand)command, configuracao);
}
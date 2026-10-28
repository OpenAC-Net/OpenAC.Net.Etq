using System;
using System.Text;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Interpreter;
using OpenAC.Net.Etq.Interpreter.Ppla;
using OpenAC.Net.Etq.Interpreter.Epl2;
using OpenAC.Net.Etq.Interpreter.ZplII;
using OpenAC.Net.Etq.Interpreter.Tspl;
using OpenAC.Net.Etq.Interpreter.EscLabel;
namespace OpenAC.Net.Etq;

/// <summary>Cria o interpretador do protocolo e da página de código selecionados.</summary>
public static class EtqInterpreterFactory
{
    public static EtqInterpreter Create(ProtocoloEtq protocolo, PaginaCodigo paginaCodigo = PaginaCodigo.pc850)
    {
        if (!Enum.IsDefined(typeof(PaginaCodigo), paginaCodigo)) throw new ArgumentOutOfRangeException(nameof(paginaCodigo));
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding((int)paginaCodigo, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        return protocolo switch
        {
            ProtocoloEtq.Ppla => new PplaInterpreter(encoding),
            ProtocoloEtq.Pplb or ProtocoloEtq.Epl2 => new Epl2Interpreter(encoding),
            ProtocoloEtq.ZplII => new ZplIIInterpreter(encoding),
            ProtocoloEtq.Tspl => new TsplInterpreter(encoding),
            ProtocoloEtq.EscLabel => new EscLabelInterpreter(encoding),
            _ => throw new ArgumentOutOfRangeException(nameof(protocolo), protocolo, "Protocolo não suportado.")
        };
    }
}

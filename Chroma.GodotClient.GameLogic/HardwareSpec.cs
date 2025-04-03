using MysticClue.Chroma.GodotClient.GameLogic.Grid;
using System.Net;
using System.Text.Json.Serialization;

namespace MysticClue.Chroma.GodotClient.GameLogic;

/// <summary>
/// A specification for the hardware connected to machine.
///
/// Normally stored and read as a JSON file.
/// </summary>
public class HardwareSpec
{
    /// <summary>
    /// The local endpoint used to listen for connections from hardware.
    /// </summary>
    [JsonConverter(typeof(JsonIPEndPointConverter))]
    public IPEndPoint? ServerEndpoint { get; set; }

    /// <summary>
    /// Any grid hardware connected to this machine.
    /// </summary>
    public GridSpecs? Grid { get; set; }
}

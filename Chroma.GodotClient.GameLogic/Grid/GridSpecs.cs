using System.Net;
using System.Text.Json.Serialization;

namespace MysticClue.Chroma.GodotClient.GameLogic.Grid;

/// <summary>
/// A specification for the hardware attached to this machine.
/// 
/// Integer width and height dictate the possible positions of objects in the grid at each step.
/// (We can still animate smoothly and interpolate collisions between steps).
/// </summary>
/// <param name="Width">Number of units wide.</param>
/// <param name="Height">Number of units across.</param>
/// <param name="PixelsPerUnit">Number of output pixels per grid unit.</param>
/// <param name="ColumnWise">If set, chains of tiles are arranged in columns instead of rows.</param>
/// <param name="OutputChains">List of outputs and where they map to on the grid.</param>
public record class GridSpecs(
    int Width,
    int Height,
    int PixelsPerUnit,
    bool ColumnWise,
    PrintedList<GridSpecs.OutputChain> OutputChains
)
{
    /// <summary>
    /// Specifies where an output endpoint maps into a grid.
    /// </summary>
    /// <param name="Endpoint">Endpoint address and port.</param>
    /// <param name="ConnectedAtEnd">Whether the first tile in the chain is on the right of the grid instead of the left (or, if column-wise, bottom instead of top).</param>
    /// <param name="FirstIndex">First row (or column) of the grid covered by this chain.</param>
    /// <param name="LastIndex">Last row (or column) of the grid covered by this chain.</param>
    public record class OutputChain(
        [property: JsonConverter(typeof(JsonIPEndPointConverter))]
        IPEndPoint? Endpoint,
        string? SerialPort,
        bool ConnectedAtEnd,
        int FirstIndex,
        int LastIndex);
}

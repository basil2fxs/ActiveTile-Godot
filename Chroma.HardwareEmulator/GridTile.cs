namespace MysticClue.Chroma.HardwareEmulator;

public class GridTile : Panel
{
    public bool Pressed => _transientPressed || _lockedPressed;
    private bool _transientPressed;
    private bool _lockedPressed;

    public GridTile()
    {
        AllowDrop = true;
    }

    protected override void OnDragEnter(DragEventArgs drgevent)
    {
        _transientPressed = true;

        base.OnDragEnter(drgevent);
    }

    protected override void OnDragLeave(EventArgs e)
    {
        _transientPressed = false;

        base.OnDragLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            DoDragDrop(this, DragDropEffects.All);
        }
        else if (e.Button == MouseButtons.Right)
        {
            if (_lockedPressed)
            {
                _lockedPressed = false;
                BorderStyle = BorderStyle.FixedSingle;
            }
            else
            {
                _lockedPressed = true;
                BorderStyle = BorderStyle.Fixed3D;
            }
        }

        base.OnMouseDown(e);
    }

    protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
    {
        // The default is to show an action-specfic cursor.
        // Disabling this means we show the plain mouse cursor.
        gfbevent.UseDefaultCursors = false;

        base.OnGiveFeedback(gfbevent);
    }
}

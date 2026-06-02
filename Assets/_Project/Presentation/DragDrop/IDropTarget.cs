namespace DeadManZone.Presentation.DragDrop
{
    public interface IDropTarget
    {
        bool TryAccept(DragPayload payload);
    }
}

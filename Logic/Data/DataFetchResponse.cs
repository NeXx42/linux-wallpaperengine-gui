namespace Logic.Data;

public struct DataFetchResponse
{
    public IWorkshopEntry[]? entries;
    public Exception? exception;

    public DataFetchResponse(IWorkshopEntry[] entries)
    {
        this.entries = entries;
        exception = null;
    }

    public DataFetchResponse(Exception e)
    {
        exception = e;
        entries = null;
    }
}

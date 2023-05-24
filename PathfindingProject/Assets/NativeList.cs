using Unity.Collections;

internal class NativeList<T>
{
    private Allocator tempJob;

    public NativeList(Allocator tempJob)
    {
        this.tempJob = tempJob;
    }
}
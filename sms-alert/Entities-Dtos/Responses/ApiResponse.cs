namespace Entities_Dtos.Responses;

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public int? Length { get; init; }
    public T Data { get; init; }
    public List<string> Errors { get; init; } = new();

}

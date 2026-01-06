namespace CorpProcure.Models
{
    public class Result
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public List<string> Errors { get; init; } = new();

        public static Result Ok()
            => new() { Success = true };

        public static Result Fail(string error)
            => new() { Success = false, ErrorMessage = error };

        public static Result Fail(List<string> errors)
            => new() { Success = false, Errors = errors };
    }

    public class Result<T> : Result
    {
        public T? Data { get; init; }

        public static Result<T> Ok(T data)
            => new() { Success = true, Data = data };

        public new static Result<T> Fail(string error)
            => new() { Success = false, ErrorMessage = error };

        public new static Result<T> Fail(List<string> errors)
            => new() { Success = false, Errors = errors };
    }
}

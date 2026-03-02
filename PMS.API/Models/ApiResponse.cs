namespace PMS.API.Models;

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = "success";
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Message = "success",
            Data = data
        };
    }
}

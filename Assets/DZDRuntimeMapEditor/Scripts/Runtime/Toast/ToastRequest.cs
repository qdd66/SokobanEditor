namespace DZDMapEditor
{
    public readonly struct ToastRequest
    {
        public ToastRequest(string message, float durationSeconds)
        {
            Message = message;
            DurationSeconds = durationSeconds;
        }

        public string Message { get; }
        public float DurationSeconds { get; }
    }
}

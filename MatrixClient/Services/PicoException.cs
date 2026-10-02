namespace MatrixClient.Services;

// The matrix couldn't be reached or rejected the pixels
public class PicoException(string message, Exception? inner = null) : Exception(message, inner);

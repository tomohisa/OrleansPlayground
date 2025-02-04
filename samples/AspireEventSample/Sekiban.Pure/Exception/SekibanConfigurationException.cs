namespace Sekiban.Pure.Exception;

public class SekibanConfigurationException(string message)
    : ApplicationException(message), ISekibanException;
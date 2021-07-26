namespace MyFTP.Services
{
	public enum TransferItemStatus
	{
		None,
		Idle,
		Running,
		Canceled,
		Error,
		Completed,
	}
}
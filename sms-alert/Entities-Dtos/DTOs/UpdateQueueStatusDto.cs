using Entities_Dtos.Types;

namespace Entities_Dtos.DTOs;

public class UpdateQueueStatusDto
{
    public QueueStatus Status { get; set; }
    public string FailureReason { get; set; }
}

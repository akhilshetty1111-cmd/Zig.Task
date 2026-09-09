using MediatR;
using ZigZag.Application.Features.Tasks.Common;

namespace ZigZag.Application.Features.Tasks.Queries.GetTaskHistory;

public sealed record GetTaskHistoryQuery(Guid TaskId) : IRequest<IReadOnlyList<TaskHistoryEntryDto>>;

using System;
using System.Collections.Generic;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// プロジェクトのマイルストーンリストを保持するためのDTO。
/// </summary>
internal record ProjectMilestonesDto(List<MilestoneDto> Milestones);

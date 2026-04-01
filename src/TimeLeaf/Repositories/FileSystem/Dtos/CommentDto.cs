using System;
using System.Collections.Generic;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// コメント情報を保持するためのDTO。
/// </summary>
internal record CommentDto(Guid Id, Guid TaskId, Guid AuthorId, string Content, List<string> AttachmentLinks);

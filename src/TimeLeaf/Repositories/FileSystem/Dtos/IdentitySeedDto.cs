using System;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// ユーザー自身のアイデンティティ（種）を固定するための軽量 DTO。
/// seed.json に保存されます。
/// </summary>
public class IdentitySeedDto
{
    /// <summary>
    /// 固定されたユーザーID。
    /// </summary>
    public Guid Id { get; set; }
}

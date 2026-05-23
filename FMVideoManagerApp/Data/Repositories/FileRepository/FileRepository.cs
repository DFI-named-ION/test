using FMVideoManagerApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FMVideoManagerApp.Data.Repositories.FileRepository
{
    public sealed class FileRepository : IFileRepository
    {
        private readonly IDbContextFactory<LocalDbContext> _contextFactory;

        public FileRepository(IDbContextFactory<LocalDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public FileItem AddFile(long userId, long? parentNodeId, FileInfo file,
            string hash, long duration, int? width, int? height, string notes = "")
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //var node = new HierarchyNode
            //{
            //    UserId = userId,
            //    ParentNodeId = parentNodeId,
            //    NodeType = NodeTypes.File,
            //    Title = file.Name,
            //    SortOrder = 0,

            //    FileItem = new FileItem
            //    {
            //        NodeType = NodeTypes.File,
            //        Hash = hash,
            //        SizeBytes = file.Length,
            //        Path = file.FullName,
            //        OriginalFilename = file.Name,
            //        DurationMs = duration,
            //        Width = width,
            //        Height = height,
            //        Notes = notes
            //    }
            //};

            //db.HierarchyNodes.Add(node);
            //db.SaveChanges();

            //return node.FileItem;

            return null;
        }

        public List<FileItem> FindByHash(string hash)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.FileItems
            //    .AsNoTracking()
            //    .Where(x => x.Hash == hash)
            //    .ToList();

            return null;
        }

        public List<FileItem> GetAll()
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.FileItems
            //    .AsNoTracking()
            //    .ToList();
            return null;
        }

        public List<FileItem> GetByUserId(long userId)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.HierarchyNodes
            //    .AsNoTracking()
            //    .Where(x => x.NodeType == NodeTypes.File && x.UserId == userId)
            //    .Join(
            //        db.FileItems.AsNoTracking(),
            //        node => node.Id,
            //        file => file.NodeId,
            //        (node, file) => file
            //    )
            //    .ToList();

            //return db.FileItems
            //    .AsNoTracking()
            //    .Where(x => x.Node.NodeType == NodeTypes.File && x.Node.UserId == userId)
            //    .ToList();               // ??????
            return null;
        }
    }
}
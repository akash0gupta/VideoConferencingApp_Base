using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Common.ICommonServices
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
        Task<DbTransaction> BeginTransactionAsync();
        void Rollback();
        void Commit();
    }
}

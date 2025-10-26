using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.Interfaces.Common.ICommonServices
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
        Task<DbTransaction> BeginTransactionAsync();
        void Rollback();
        void Commit();
    }
}

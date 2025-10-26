using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Entities
{
    /// <summary>
    /// Represents an entity that supports soft deletion.
    /// </summary>
    public interface ISoftDeletedEntity
    {
        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted.
        /// </summary>
        bool IsDeleted { get; set; }
    }
}

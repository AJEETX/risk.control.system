﻿using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace risk.control.system.Data
{
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Get All Entity
        /// </summary>
        /// <returns>Entity</returns>
        IQueryable<TEntity> GetAll();

        /// <summary>
        /// Find Entity using linq expression
        /// </summary>
        /// <param name="predicate">linq expression</param>
        /// <returns>Entity</returns>
        TEntity FindByExpression(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Find Entities using linq expression
        /// </summary>
        /// <param name="predicate">linq expression</param>
        /// <returns>List of entities</returns>
        IQueryable<TEntity> FindManyByExpression(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Find Entity using id
        /// </summary>
        /// <param name="id">Id of entity</param>
        /// <returns>Entity</returns>
        TEntity FindById(Guid id);

        /// <summary>
        /// Insert Entity
        /// </summary>
        /// <param name="entity">Entity</param>
        void Insert(TEntity entity);

        /// <summary>
        /// Update Entity
        /// </summary>
        /// <param name="entity">Entity</param>
        void Update(TEntity entity);

        /// <summary>
        /// Delete Entity
        /// </summary>
        /// <param name="entity">Entity</param>
        void Delete(TEntity entity);

        /// <summary>
        /// Save Changes
        /// </summary>
        void SaveChanges();
    }
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private DbSet<TEntity> _entities;

        #endregion

        #region Constructor

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _entities = context.Set<TEntity>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get All Entity
        /// </summary>
        /// <returns>Entity</returns>
        public IQueryable<TEntity> GetAll()
        {
            return _entities.AsNoTracking();
        }

        /// <summary>
        /// Find Entity using linq expression
        /// </summary>
        /// <param name="predicate">linq expression</param>
        /// <returns>Entity</returns>
        public TEntity FindByExpression(Expression<Func<TEntity, bool>> predicate)
        {
            return _entities
                .AsNoTracking()
                .SingleOrDefault(predicate);
        }

        /// <summary>
        /// Find Entities using linq expression
        /// </summary>
        /// <param name="predicate">linq expression</param>
        /// <returns>List of entities</returns>
        public IQueryable<TEntity> FindManyByExpression(Expression<Func<TEntity, bool>> predicate)
        {
            return _entities
                .AsNoTracking()
                .Where(predicate);
        }

        /// <summary>
        /// Find Entity using id
        /// </summary>
        /// <param name="id">Id of entity</param>
        /// <returns>Entity</returns>
        public TEntity FindById(Guid id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert Entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public void Insert(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                _entities.Add(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Update Entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public void Update(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                _context.Entry(entity).State = EntityState.Modified;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Delete Entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public void Delete(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                _entities.Remove(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Save Changes
        /// </summary>
        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        #endregion
    }
}

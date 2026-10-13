using CathermanCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace CathermanCore.EntityFramework;

/// <summary>
/// Provides extension methods for Entity Framework Core <see cref="DbContext"/> and <see cref="ModelBuilder"/>.
/// </summary>
public static class EntityFrameworkExtensions
{
    #region AddIfMissing

    /// <summary>
    /// Adds an entity to the context if it is detached and not yet persisted (Id == 0), or attaches it if it already exists.
    /// </summary>
    /// <typeparam name="T">The entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The entity instance to add or attach.</param>
    public static void AddIfMissing<T>(this DbContext context, T entity) where T : class, IEntity
    {
        if (entity == null)
            return;

        if (context.Entry(entity).State == EntityState.Detached)
            if (entity.Id == 0 && context.Entry(entity).State != EntityState.Added)
                context.Add(entity);
            else
                context.Attach(entity);
    }

    #endregion

    #region EntitiesOfType

    /// <summary>
    /// Applies a configuration action to all entity types registered in the model that implement or derive from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target interface or base type to match against registered entities.</typeparam>
    /// <param name="builder">The model builder instance.</param>
    /// <param name="buildAction">The configuration action to execute for each matching entity type builder.</param>
    /// <returns>The updated <see cref="ModelBuilder"/> instance.</returns>
    public static ModelBuilder EntitiesOfType<T>(this ModelBuilder builder, Action<EntityTypeBuilder> buildAction) where T : class
    {
        return builder.EntitiesOfType(typeof(T), buildAction);
    }

    /// <summary>
    /// Applies a configuration action to all entity types registered in the model that derive from or implement the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="builder">The model builder instance.</param>
    /// <param name="type">The target <see cref="Type"/> to match against registered entity CLR types.</param>
    /// <param name="buildAction">The configuration action to execute for each matching entity type builder.</param>
    /// <returns>The updated <see cref="ModelBuilder"/> instance.</returns>
    public static ModelBuilder EntitiesOfType(this ModelBuilder builder, Type type, Action<EntityTypeBuilder> buildAction)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
            if (type.IsAssignableFrom(entityType.ClrType))
                buildAction(builder.Entity(entityType.ClrType));

        return builder;
    }

    #endregion

    #region Detach
    /// <summary>
    /// Sets the state of the specified entity entry to <see cref="EntityState.Detached"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The tracked or untracked entity instance to detach.</param>
    public static void Detach(this DbContext context, object entity)
    {
        var entry = context.Entry(entity);
        if (entry != null)
            entry.State = EntityState.Detached;
    }

    #endregion

    #region FindAllAsync
    /// <summary>
    /// Asynchronously retrieves all entities of type <typeparamref name="T"/> matching the collection of primary key IDs.
    /// </summary>
    /// <typeparam name="T">The entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="keyValues">The collection of primary key integer IDs to query.</param>
    /// <returns>A task representing the asynchronous operation that yields an enumerable sequence of matching entities.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="keyValues"/> is <c>null</c>.</exception>
    public static async Task<IEnumerable<T>> FindAllAsync<T>(
        this DbContext context,
        IEnumerable<int> keyValues) where T : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(keyValues);

        var keyList = keyValues as List<int> ?? keyValues.ToList();
        if (keyList.Count == 0)
            return [];

        return await context.Set<T>()
            .Where(e => keyList.Contains(e.Id))
            .ToListAsync();
    }
    /// <summary>
    /// Asynchronously retrieves all entities of a dynamic runtime <paramref name="type"/> matching the collection of primary key IDs.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="type">The runtime entity <see cref="Type"/> to query. Must implement <see cref="IEntity"/>.</param>
    /// <param name="keyValues">The collection of primary key integer IDs to query.</param>
    /// <returns>A task representing the asynchronous operation that yields an enumerable sequence of matching entities as <see cref="IEntity"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="type"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="type"/> does not implement <see cref="IEntity"/>.</exception>
    public static async Task<IEnumerable<IEntity>> FindAllAsync(
        this DbContext context,
        Type type,
        IEnumerable<int> keyValues)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(type);

        if (!typeof(IEntity).IsAssignableFrom(type))
            throw new NotSupportedException($"Type {type.Name} does not implement {nameof(IEntity)}.");

        var genericMethod = typeof(EntityFrameworkExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(FindAllAsync) && m.IsGenericMethod)
            .MakeGenericMethod(type);

        var task = (Task)genericMethod.Invoke(null, [context, keyValues])!;
        await task;

        var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result))!;
        var enumerableResult = (IEnumerable)resultProperty.GetValue(task)!;

        return enumerableResult.Cast<IEntity>();
    }

    #endregion

    #region DeleteAsync

    /// <summary>
    /// Asynchronously performs a soft delete on an entity with the specified ID by setting its <see cref="IEntity.IsLive"/> property to <c>false</c>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="id">The primary key integer ID of the entity to mark for soft deletion.</param>
    /// <param name="condition">An optional condition predicate that must evaluate to <c>true</c> for the soft delete to proceed.</param>
    /// <returns>A task representing the asynchronous operation that yields <c>true</c> if the entity was successfully updated and saved; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
    public static async Task<bool> DeleteAsync<TEntity>(
        this DbContext context,
        int id,
        Func<TEntity, bool>? condition = null) where TEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(context);
        var entity = await context.FindAsync<TEntity>(id);
        return await context.DeleteAsync(entity, condition);
    }

    /// <summary>
    /// Asynchronously performs a soft delete on an entity with the specified ID by setting its <see cref="IEntity.IsLive"/> property to <c>false</c>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The entity to mark for soft deletion.</param>
    /// <param name="condition">An optional condition predicate that must evaluate to <c>true</c> for the soft delete to proceed.</param>
    /// <returns>A task representing the asynchronous operation that yields <c>true</c> if the entity was successfully updated and saved; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
    public static async Task<bool> DeleteAsync<TEntity>(
        this DbContext context,
        TEntity? entity,
        Func<TEntity, bool>? condition = null) where TEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(context);

        if (entity == null)
            return false;

        if (condition != null && !condition(entity))
            return false;

        entity.IsLive = false;
        await context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Asynchronously performs a soft delete on multiple entities matching the provided IDs by setting their <see cref="IEntity.IsLive"/> property to <c>false</c>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="ids">The collection of primary key integer IDs of entities to mark for soft deletion.</param>
    /// <param name="condition">An optional condition predicate that must evaluate to <c>true</c> for an entity to be soft deleted.</param>
    /// <returns>A task representing the asynchronous operation that yields <c>true</c> if at least one entity was updated and saved; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="ids"/> is <c>null</c>.</exception>
    public static async Task<bool> DeleteAsync<TEntity>(
        this DbContext context,
        IEnumerable<int> ids,
        Func<TEntity, bool>? condition = null) where TEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ids);
        var entities = await context.FindAllAsync<TEntity>(ids);
        return await context.DeleteAsync(entities, condition);
    }

    /// <summary>
    /// Asynchronously performs a soft delete on multiple entities matching the provided IDs by setting their <see cref="IEntity.IsLive"/> property to <c>false</c>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entities">The collection of entities to mark for soft deletion.</param>
    /// <param name="condition">An optional condition predicate that must evaluate to <c>true</c> for an entity to be soft deleted.</param>
    /// <returns>A task representing the asynchronous operation that yields <c>true</c> if at least one entity was updated and saved; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="ids"/> is <c>null</c>.</exception>
    public static async Task<bool> DeleteAsync<TEntity>(
        this DbContext context,
        IEnumerable<TEntity> entities,
        Func<TEntity, bool>? condition = null) where TEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entities);

        var list = entities.ToList();
        if (list.Count == 0)
            return false;

        var entitiesToDelete = condition != null
            ? list.Where(condition).ToList()
            : list;

        if (entitiesToDelete.Count == 0)
            return false;

        foreach (var entity in entitiesToDelete)
            entity.IsLive = false;

        await context.SaveChangesAsync();

        return true;
    }


    #endregion

    #region Load with singular reference

    /// <summary>
    /// Synchronously loads a single navigation reference property for an entity if it is not already loaded.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TReference">The referenced entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="referenceFunc">A lambda expression specifying the reference navigation property to load.</param>
    public static void Load<TEntity, TReference>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, TReference?>> referenceFunc)
        where TEntity : class, IEntity
        where TReference : class, IEntity
        => context.LoadAsync(entity, referenceFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a single navigation reference property for an entity if it is not already loaded.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TReference">The referenced entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="referenceFunc">A lambda expression specifying the reference navigation property to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static async Task LoadAsync<TEntity, TReference>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, TReference?>> referenceFunc)
        where TEntity : class, IEntity
        where TReference : class, IEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity.Id == 0)
            return;

        var reference = context.Entry(entity).Reference(referenceFunc);

        if (!reference.IsLoaded)
            await reference.LoadAsync();
    }

    /// <summary>
    /// Synchronously loads a single navigation reference property for a sequence of entities if not already loaded.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TReference">The referenced entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entities">The collection of root entity instances.</param>
    /// <param name="referenceFunc">A lambda expression specifying the reference navigation property to load.</param>
    public static void Load<TEntity, TReference>(
        this DbContext context,
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, TReference?>> referenceFunc)
        where TEntity : class, IEntity
        where TReference : class, IEntity
        => context.LoadAsync(entities, referenceFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a single navigation reference property for a sequence of entities if not already loaded.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TReference">The referenced entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entities">The collection of root entity instances.</param>
    /// <param name="referenceFunc">A lambda expression specifying the reference navigation property to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when any element in <paramref name="entities"/> is <c>null</c>.</exception>
    public static async Task LoadAsync<TEntity, TReference>(
        this DbContext context,
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, TReference?>> referenceFunc)
        where TEntity : class, IEntity
        where TReference : class, IEntity
    {
        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.Id == 0)
                continue;

            var reference = context.Entry(entity).Reference(referenceFunc);

            if (!reference.IsLoaded)
                await reference.LoadAsync();
        }
    }

    /// <summary>
    /// Synchronously loads a single navigation reference property and an included child collection navigation.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TReference">The referenced entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TInclude">The child entity collection type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="referenceFunc">A lambda expression specifying the reference navigation property.</param>
    /// <param name="includeFunc">A lambda expression specifying the child collection navigation to include on the reference.</param>
    public static void Load<TEntity, TReference, TInclude>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, TReference?>> referenceFunc,
        Expression<Func<TReference, IEnumerable<TInclude>>> includeFunc)
        where TEntity : class, IEntity
        where TReference : class, IEntity
        where TInclude : class, IEntity
        => context.LoadAsync(entity, referenceFunc, includeFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a single navigation reference property and an included child collection navigation.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TReference">The referenced entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TInclude">The child entity collection type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="referenceFunc">A lambda expression specifying the reference navigation property.</param>
    /// <param name="includeFunc">A lambda expression specifying the child collection navigation to include on the reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static async Task LoadAsync<TEntity, TReference, TInclude>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, TReference?>> referenceFunc,
        Expression<Func<TReference, IEnumerable<TInclude>>> includeFunc)
        where TEntity : class, IEntity
        where TReference : class, IEntity
        where TInclude : class, IEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity.Id == 0)
            return;

        var referenceEntry = context.Entry(entity).Reference(referenceFunc);

        if (!referenceEntry.IsLoaded)
            await referenceEntry.Query().Include(includeFunc).LoadAsync();
    }

    /// <summary>
    /// Synchronously loads a single navigation reference property, an included child collection, and a nested navigation property via ThenInclude.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TReference">The referenced entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TInclude">The child entity collection type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TThenInclude">The nested entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="referenceFunc">A lambda expression specifying the reference navigation property.</param>
    /// <param name="includeFunc">A lambda expression specifying the child collection navigation to include.</param>
    /// <param name="thenIncludeFunc">A lambda expression specifying the nested navigation property to include.</param>
    public static void Load<TEntity, TReference, TInclude, TThenInclude>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, TReference?>> referenceFunc,
        Expression<Func<TReference, IEnumerable<TInclude>>> includeFunc,
        Expression<Func<TInclude, TThenInclude>> thenIncludeFunc)
        where TEntity : class, IEntity
        where TReference : class, IEntity
        where TInclude : class, IEntity
        where TThenInclude : class, IEntity
        => context.LoadAsync(entity, referenceFunc, includeFunc, thenIncludeFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a single navigation reference property, an included child collection, and a nested navigation property via ThenInclude.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TReference">The referenced entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TInclude">The child entity collection type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TThenInclude">The nested entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="referenceFunc">A lambda expression specifying the reference navigation property.</param>
    /// <param name="includeFunc">A lambda expression specifying the child collection navigation to include.</param>
    /// <param name="thenIncludeFunc">A lambda expression specifying the nested navigation property to include.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static async Task LoadAsync<TEntity, TReference, TInclude, TThenInclude>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, TReference?>> referenceFunc,
        Expression<Func<TReference, IEnumerable<TInclude>>> includeFunc,
        Expression<Func<TInclude, TThenInclude>> thenIncludeFunc)
        where TEntity : class, IEntity
        where TReference : class, IEntity
        where TInclude : class, IEntity
        where TThenInclude : class, IEntity
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity.Id == 0)
            return;

        var referenceEntry = context.Entry(entity).Reference(referenceFunc);

        if (!referenceEntry.IsLoaded)
            await referenceEntry.Query().Include(includeFunc).ThenInclude(thenIncludeFunc).LoadAsync();
    }

    #endregion

    #region Load with collection

    /// <summary>
    /// Synchronously loads a collection navigation property for each entity in a collection if not already loaded.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entities">The collection of root entity instances.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property to load.</param>
    public static void Load<TEntity, TCollection>(
        this DbContext context,
        ICollection<TEntity> entities,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
        => context.LoadAsync(entities, collectionFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a collection navigation property for each entity in a collection if not already loaded.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entities">The collection of root entity instances.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/>, <paramref name="collectionFunc"/>, or <paramref name="context"/> is <c>null</c>.</exception>
    /// <exception cref="Exception">Thrown when an error occurs during loading of the navigation collection.</exception>
    public static async Task LoadAsync<TEntity, TCollection>(
        this DbContext context,
        ICollection<TEntity> entities,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (collectionFunc == null) throw new ArgumentNullException(nameof(collectionFunc));
        if (context == null) throw new ArgumentNullException(nameof(context));

        foreach (var entity in entities)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.Id == 0)
                continue;

            var collection = context.Entry(entity).Collection(collectionFunc);

            if (collection.IsLoaded)
                continue;

            try
            { await collection.LoadAsync(); }
            catch (Exception ex)
            { throw new Exception($"Unable to load entity: {typeof(TEntity).Name}", ex); }
        }
    }

    /// <summary>
    /// Synchronously loads a collection navigation property for an entity if it is not already loaded.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property to load.</param>
    public static void Load<TEntity, TCollection>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
        => context.LoadAsync(entity, collectionFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a collection navigation property for an entity if it is not already loaded.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property to load.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/>, <paramref name="collectionFunc"/>, or <paramref name="context"/> is <c>null</c>.</exception>
    /// <exception cref="Exception">Thrown when an error occurs during loading of the navigation collection.</exception>
    public static async Task LoadAsync<TEntity, TCollection>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (collectionFunc == null) throw new ArgumentNullException(nameof(collectionFunc));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var collection = context.Entry(entity).Collection(collectionFunc);

        if (collection.IsLoaded)
            return;

        try
        { await collection.LoadAsync(); }
        catch (Exception ex)
        { throw new Exception($"Unable to load entity: {typeof(TEntity).Name}", ex); }
    }

    /// <summary>
    /// Synchronously loads a collection navigation property for an entity and includes child navigation properties.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TInclude">The child entity type to include implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property.</param>
    /// <param name="includeFunc">A lambda expression specifying the child navigation property to include.</param>
    public static void Load<TEntity, TCollection, TInclude>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc,
        Expression<Func<TCollection, IEnumerable<TInclude>>> includeFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
        where TInclude : class, IEntity
        => context.LoadAsync(entity, collectionFunc, includeFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a collection navigation property for an entity and includes child navigation properties.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TInclude">The child entity type to include implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property.</param>
    /// <param name="includeFunc">A lambda expression specifying the child navigation property to include.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/>, <paramref name="collectionFunc"/>, <paramref name="context"/>, or <paramref name="includeFunc"/> is <c>null</c>.</exception>
    /// <exception cref="Exception">Thrown when an error occurs during loading of the navigation collection.</exception>
    public static async Task LoadAsync<TEntity, TCollection, TInclude>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc,
        Expression<Func<TCollection, IEnumerable<TInclude>>> includeFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
        where TInclude : class, IEntity
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entity.Id == 0) return;
        if (collectionFunc == null) throw new ArgumentNullException(nameof(collectionFunc));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (includeFunc == null) throw new ArgumentNullException(nameof(includeFunc));

        try
        {
            await context.Entry(entity)
                .Collection(collectionFunc)
                .Query()
                .Include(includeFunc)
                .LoadAsync();
        }
        catch (Exception ex)
        { throw new Exception($"Unable to load entity: {typeof(TEntity).Name}", ex); }
    }

    /// <summary>
    /// Synchronously loads a filtered collection navigation property for an entity based on a specified predicate.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property.</param>
    /// <param name="whereFunc">A lambda expression predicate used to filter the loaded collection elements.</param>
    public static void Load<TEntity, TCollection>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc,
        Expression<Func<TCollection, bool>> whereFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
        => context.LoadAsync(entity, collectionFunc, whereFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a filtered collection navigation property for an entity based on a specified predicate.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property.</param>
    /// <param name="whereFunc">A lambda expression predicate used to filter the loaded collection elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/>, <paramref name="collectionFunc"/>, <paramref name="context"/>, or <paramref name="whereFunc"/> is <c>null</c>.</exception>
    /// <exception cref="Exception">Thrown when an error occurs during loading of the navigation collection.</exception>
    public static async Task LoadAsync<TEntity, TCollection>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc,
        Expression<Func<TCollection, bool>> whereFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entity.Id == 0) return;
        if (collectionFunc == null) throw new ArgumentNullException(nameof(collectionFunc));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (whereFunc == null) throw new ArgumentNullException(nameof(whereFunc));

        try
        {
            await context.Entry(entity)
                .Collection(collectionFunc)
                .Query()
                .Where(whereFunc)
                .LoadAsync();
        }
        catch (Exception ex)
        { throw new Exception($"Unable to load entity: {typeof(TEntity).Name}", ex); }
    }

    /// <summary>
    /// Synchronously loads a collection navigation property for an entity, including child collections and nested collections via ThenInclude.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TInclude">The child entity collection type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TThenInclude">The nested entity collection type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property.</param>
    /// <param name="includeFunc">A lambda expression specifying the child collection navigation property.</param>
    /// <param name="thenIncludeFunc">A lambda expression specifying the nested collection navigation property.</param>
    public static void Load<TEntity, TCollection, TInclude, TThenInclude>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc,
        Expression<Func<TCollection, IEnumerable<TInclude>>> includeFunc,
        Expression<Func<TInclude, IEnumerable<TThenInclude>>> thenIncludeFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
        where TInclude : class, IEntity
        where TThenInclude : class, IEntity
        => context.LoadAsync(entity, collectionFunc, includeFunc, thenIncludeFunc).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously loads a collection navigation property for an entity, including child collections and nested collections via ThenInclude.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TCollection">The elements' type within the collection implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TInclude">The child entity collection type implementing <see cref="IEntity"/>.</typeparam>
    /// <typeparam name="TThenInclude">The nested entity collection type implementing <see cref="IEntity"/>.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="entity">The root entity instance.</param>
    /// <param name="collectionFunc">A lambda expression specifying the collection navigation property.</param>
    /// <param name="includeFunc">A lambda expression specifying the child collection navigation property.</param>
    /// <param name="thenIncludeFunc">A lambda expression specifying the nested collection navigation property.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/>, <paramref name="collectionFunc"/>, <paramref name="context"/>, <paramref name="includeFunc"/>, or <paramref name="thenIncludeFunc"/> is <c>null</c>.</exception>
    /// <exception cref="Exception">Thrown when an error occurs during loading of the navigation collection.</exception>
    public static async Task LoadAsync<TEntity, TCollection, TInclude, TThenInclude>(
        this DbContext context,
        TEntity entity,
        Expression<Func<TEntity, IEnumerable<TCollection>>> collectionFunc,
        Expression<Func<TCollection, IEnumerable<TInclude>>> includeFunc,
        Expression<Func<TInclude, IEnumerable<TThenInclude>>> thenIncludeFunc)
        where TEntity : class, IEntity
        where TCollection : class, IEntity
        where TInclude : class, IEntity
        where TThenInclude : class, IEntity
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entity.Id == 0) return;
        if (collectionFunc == null) throw new ArgumentNullException(nameof(collectionFunc));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (includeFunc == null) throw new ArgumentNullException(nameof(includeFunc));
        if (thenIncludeFunc == null) throw new ArgumentNullException(nameof(thenIncludeFunc));

        try
        {
            await context.Entry(entity)
                .Collection(collectionFunc)
                .Query()
                .Include(includeFunc)
                .ThenInclude(thenIncludeFunc)
                .LoadAsync();
        }
        catch (Exception ex)
        { throw new Exception($"Unable to load entity: {typeof(TEntity).Name}", ex); }
    }

    #endregion

}
public interface IEntityMapper<TModel, TViewModel>
{
    TViewModel ToViewModel(TModel model);
    TModel ToModel(TViewModel vm);
}
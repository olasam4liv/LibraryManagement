using FluentValidation;
using LibraryManagementSystem.Dto;

namespace LibraryManagementSystem.Dto
{
    public class BookDtoValidator : AbstractValidator<BookDto>
    {
        public BookDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200);
            RuleFor(x => x.Author)
                .NotEmpty().WithMessage("Author is required.")
                .MaximumLength(100);
            RuleFor(x => x.ISBN)
                .NotEmpty().WithMessage("ISBN is required.")
                .Length(10, 20);
            RuleFor(x => x.PublishedDate)
                .NotEmpty().WithMessage("Published date is required.");
        }
    }
}

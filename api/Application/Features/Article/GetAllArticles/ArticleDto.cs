﻿namespace FeedbackAnalyzer.Application.Features.Article.GetAllArticles;

public class ArticleDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Creator { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
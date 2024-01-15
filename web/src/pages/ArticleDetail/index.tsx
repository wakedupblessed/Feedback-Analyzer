import React, { useEffect, useState } from "react";

import { useNavigate, useParams } from "react-router-dom";
import { toast } from "react-toastify";

import {
  Title,
  PageContainer,
  Header,
  HeaderTitle,
  HeaderActions,
  ActionItem,
  PageContent,
  Text,
  Edited,
  CommentsSectionContainer,
} from "./styles";

import {
  ArticleDetailDto,
  deleteArticle,
  getArticleById,
} from "../../api/articles";
import useAuthContext from "../../context/hooks";
import { getFormatedDate } from "../../utils/date";
import CommentsSection from "../../components/CommentSection";
import ArticleStatistic from "../../components/ArticleStatistic";

import { postComment, updateComment, deleteComment } from "../../api/comments";
import { StyledButton } from "../../components/GlobalStyles";

const ArticleDetail = () => {
  const [comment, setComment] = useState("");
  const [article, setArticle] = useState<ArticleDetailDto | null>(null);

  const { articleId } = useParams();
  const navigate = useNavigate();
  const { user, jwtTokens } = useAuthContext();

  const handleCreateComment = async (value: string) => {
    if (articleId && user && jwtTokens) {
      await postComment(articleId, value, user?.id!!, jwtTokens?.accessToken!!);
      await getResponse();
    }
  };

  const handleUpdateComment = async (text: string, commentId: string) => {
    if (articleId && user && jwtTokens) {
      await updateComment(articleId, text, commentId, jwtTokens.accessToken);
      await getResponse();
    }
  };

  const handleDeleteComment = async (commentId: string) => {
    if (articleId && user && jwtTokens) {
      await deleteComment(articleId, commentId, jwtTokens.accessToken);
      await getResponse();
    }
  };

  async function getResponse() {
    if (articleId) {
      const result = await getArticleById(articleId);
      setArticle(result as ArticleDetailDto);
    }
  }

  const handleDeleteArticle = async () => {
    if (articleId && jwtTokens) {
      const result = await deleteArticle(articleId, jwtTokens.accessToken);
      if (!result) {
        toast.success("Article deleted successfully!");
        navigate("/");
      }
    }
  };

  useEffect(() => {
    getResponse();
  }, [articleId]);

  return (
    <PageContainer>
      {article && (
        <>
          <Header>
            <HeaderTitle>{article.title}</HeaderTitle>
            <ArticleStatistic articleId={article.id} />
            <HeaderActions>
              <Edited>edited - {getFormatedDate(article.updated)}</Edited>
              {user && (
                <>
                  <ActionItem>
                    <StyledButton
                      width="75px"
                      padding="8px 12px"
                      onClick={() => navigate("/articles/edit/" + articleId)}
                    >
                      Edit
                    </StyledButton>
                  </ActionItem>
                  <ActionItem>
                    <StyledButton
                      width="75px"
                      padding="8px 12px"
                      onClick={handleDeleteArticle}
                    >
                      Delete
                    </StyledButton>
                  </ActionItem>
                </>
              )}
            </HeaderActions>
          </Header>
          <PageContent>
            <Title>{article.title}</Title>
            <Text>{article.content}</Text>
            <CommentsSectionContainer>
              <CommentsSection
                comments={article.comments || []}
                comment={comment}
                setComment={setComment}
                updateComment={handleUpdateComment}
                deleteComment={handleDeleteComment}
                postComment={handleCreateComment}
              />
            </CommentsSectionContainer>
          </PageContent>
        </>
      )}
    </PageContainer>
  );
};

export default ArticleDetail;

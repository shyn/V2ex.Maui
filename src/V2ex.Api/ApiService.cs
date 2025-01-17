﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace V2ex.Api;

public class ApiService
{
    public ApiService(IHttpClientFactory httpClientFactory, ILogger<ApiService> logger)
    {
        this.HttpClient = httpClientFactory.CreateClient("api");

        if (this.HttpClient.BaseAddress == null)
        {
            this.HttpClient.BaseAddress = new Uri(UrlUtilities.BASE_URL);
        }
        this.Logger = logger;
    }

    private HttpClient HttpClient { get; }
    public ILogger<ApiService> Logger { get; }

    public async Task<DailyHotInfo?> GetDailyHot()
    {
        var url = "/api/topics/hot.json";

        this.Logger.LogDebug("GetDailyHot: {0}", url);
        var response = await this.HttpClient.GetAsync(url);
        this.Logger.LogDebug("GetDailyHot: {0}", response.StatusCode);

        var dailyHotInfo = await response.ReadFromJson<DailyHotInfo>();
        return dailyHotInfo;
    }

    public async Task<NodeInfo?> GetNodeInfo(string nodeName)
    {
        var url = $"/api/nodes/show.json?name={nodeName}";
        var response = await this.HttpClient.GetAsync(url);
        var nodeInfo = await response.ReadFromJson<NodeInfo>();
        return nodeInfo;
    }

    public async Task<NodePageInfo?> GetNodePageInfo(string nodeName, int page = 1)
    {
        var url = $"/go/{nodeName}?p={page}";
        var response = await this.HttpClient.GetAsync(url);
        var nodePageInfo = await response.GetEncapsulatedData<NodePageInfo>(this.Logger);
        return nodePageInfo;
    }

    public async Task<NodesInfo?> GetNodesInfo()
    {
        var url = "/api/nodes/s2.json";
        var response = await this.HttpClient.GetAsync(url);
        var nodesInfo = await response.ReadFromJson<NodesInfo>();
        return nodesInfo;
    }

    public async Task<NodesInfo?> GetNodesInfo2()
    {
        var url = "api/nodes/list.json?fields=name,title,topics,aliases&sort_by=topics&reverse=1";
        var response = await this.HttpClient.GetAsync(url);
        var nodesInfo = await response.ReadFromJson<NodesInfo>();
        return nodesInfo;
    }

    public async Task<MemberInfo?> GetMemberInfo(string username)
    {
        var url = $"/api/members/show.json?username={username}";
        var response = await this.HttpClient.GetAsync(url);
        var memberInfo = await response.ReadFromJson<MemberInfo>();
        return memberInfo;
    }

    public async Task<SoV2EXSearchResultInfo?> Search(string keyword, int from = 0, string sort = "created")
    {
        // https://github.com/bynil/sov2ex/blob/v2/API.md
        var queryString = new Dictionary<string, string>
        {
            { "q", keyword },
            { "from", from.ToString() },
            { "sort", sort },
            { "size", 50.ToString() }
        };

        var url = $"https://www.sov2ex.com/api/search?{EncodeQuerystring(queryString)}";
        var response = await this.HttpClient.GetAsync(url);

        var result = await response.ReadFromJson<SoV2EXSearchResultInfo>();
        return result;
    }

    private static string EncodeQuerystring(Dictionary<string, string> queryString)
    {
        return string.Join("&", queryString.Select(x => $"{x.Key}={x.Value}"));
    }

    public async Task<NewsInfo> GetTabTopics(string? tab= null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/?tab=" + tab);
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/");
        var response = await this.HttpClient.SendAsync(request);
        var newsInfo = await response.GetEncapsulatedData<NewsInfo>(this.Logger);
        return newsInfo;
    }

    public async Task<NewsInfo> GetRecentTopics()
    {
        var url = "/recent";
        var response = await this.HttpClient.GetAsync(url);
        var newsInfo = await response.GetEncapsulatedData<NewsInfo>(this.Logger);
        return newsInfo;
    }

    public async Task<TagInfo> GetTagInfo(string tagName, int page = 1)
    {
        var url = $"/tag/{tagName}?p={page}";
        var response = await this.HttpClient.GetAsync(url);
        var tagInfo =  await response.GetEncapsulatedData<TagInfo>(this.Logger);
        return tagInfo;
    }

    public async Task<LoginParameters> GetLoginParameters()
    {
        var url = "/signin?next=/";
        var response = await this.HttpClient.GetAsync(url);
        var loginParameters = await response.GetEncapsulatedData<LoginParameters, RestrictedProblem>((error) =>
        {
            if (error.IsRestricted())
            {
                throw new InvalidOperationException(error.RestrictedContent);
            }
        }, this.Logger);

        return loginParameters;
    }

    public async Task<byte[]> GetCaptchaImage(LoginParameters loginParameters)
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var url = $"{loginParameters.Captcha}?once={loginParameters.Once}&now={now}";
        var response = await this.HttpClient.GetAsync(url);
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<NewsInfo> Login(
        LoginParameters loginParameters,
        string username,
        string password,
        string captcha)
    {
        var url = "/signin";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { loginParameters.NameParameter, username },
                { loginParameters.PasswordParameter, password },
                { loginParameters.CaptchaParameter, captcha},
                { "once", loginParameters.Once },
                { "next", "/" },
            })
        };

        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/signin");
        var response = await this.HttpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Found
            && response.Headers.Location != request.RequestUri)
        {
            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
            response = await this.HttpClient.SendAsync(redirectRequest);
        }
        else
        {
            var problem = await response.GetEncapsulatedData<LoginProblem>(this.Logger);
            throw new InvalidOperationException(string.Join(" ", problem.Errors));
        }

        var newsInfo = await response.GetEncapsulatedData<NewsInfo>(this.Logger);
        return newsInfo;
    }

    public async Task<TopicInfo> GetTopicDetail(string topicId, int page = 1)
    {
        var url = $"/t/{topicId}?p={page}";
        var response = await this.HttpClient.GetAsync(url);

        var topicInfo = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return topicInfo;
    }

    public async Task<NotificationInfo?> GetNotifications(int page = 1)
    {
        var url = $"/notifications?p={page}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<NotificationInfo>(this.Logger);
        return result;
    }

    public async Task<FollowingInfo?> GetFollowingInfo(int page = 1)
    {
        var url = $"/my/following?p={page}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<FollowingInfo>(this.Logger);
        return result;
    }

    public async Task<FavoriteTopicsInfo?> GetFavoriteTopics(int page = 1)
    {
        var url = $"/my/topics?p={page}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<FavoriteTopicsInfo>(this.Logger);
        return result;
    }

    public async Task<FavoriteNodeInfo> GetFavoriteNodes()
    {
        var url = "/my/nodes";
        var response = await this.HttpClient.GetAsync(url);

        var nodeInfo = await response.GetEncapsulatedData<FavoriteNodeInfo>(this.Logger);

        foreach (var item in nodeInfo.Items)
        {
            item.Image = UrlUtilities.CompleteUrl(item.Image);
        }
        return nodeInfo;
    }

    public async Task<NodesNavInfo> GetNodesNavInfo()
    {
        var url = "/";
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<NodesNavInfo>(this.Logger);
        return result;
    }

    public async Task<MemberPageInfo> GetUserPageInfo(string username)
    {
        var url = $"/member/{username}";

        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<MemberPageInfo>(this.Logger);
        return result;
    }

    public Task<BingSearchResultInfo> BingSearch(string url)
    {
        throw new NotImplementedException();
    }

    public async Task<CreateTopicParameter> GetCreateTopicParameter()
    {
        var url = "/new";
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<CreateTopicParameter>(this.Logger);
        return result;
    }

    public async Task<TopicInfo> PostTopic(string title, string content,
        string nodeId, string once)
    {
        var url = "/new";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "title", title},
                { "syntax", "default"},
                { "content", content },
                { "node_name", nodeId },
                { "once", once },
            })
        };
        var response = await this.HttpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Found
            && response.Headers.Location != request.RequestUri)
        {
            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
            response = await this.HttpClient.SendAsync(redirectRequest);
        }
        else if( response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var problem = await response.GetEncapsulatedData<Problem>(this.Logger);
            throw new CreateTopicException(problem);
        }
        else
        {
            throw new InvalidOperationException(response.ReasonPhrase);
        }
        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }

    public async Task<AppendTopicParameter> GetAppendTopicParameter(string topicId)
    {
        var url = $"/t/{topicId}/append";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/t/{topicId}");
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<AppendTopicParameter>(this.Logger);
        return result;
    }

    public async Task<TopicInfo> AppendTopic(string topicId,
        string once,
        string content)
    {
        var url = $"/t/{topicId}/append";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "content", content },
                { "once", once },
            })
        };
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }

    public async Task<UnitInfo> ThanksReplier(string replyId, string once)
    {
        var url = $"/thank/reply/{replyId}?once={once}";
        var response = await this.HttpClient.PostAsync(url, null);
        var content = await response.Content.ReadAsStringAsync();
        var result = UnitInfo.Parse(content);
        return result;
    }

    public async Task<ThanksResult?> ThankCreator(string topicId, string once)
    {
        var url = $"/thank/topic/{topicId}?once={once}";
        var response = await this.HttpClient.PostAsync(url, null);
        var result = await response.ReadFromJson<ThanksResult>();
        return result;
    }

 

    public async Task<ThanksInfo> ThanksMoney()
    {
        var url = "/ajax/money";
        var response = await this.HttpClient.PostAsync(url, null);
        var result = await response.GetEncapsulatedData<ThanksInfo>(this.Logger);
        return result;
    }

    public async Task<TopicInfo> IgnoreTopic(string topicId, string once)
    {
        var url = $"/ignore/topic/{topicId}?once={once}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/t/{topicId}");

        var response = await this.HttpClient.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Found
            && response.Headers.Location != null)
        {
            var topicUrl = $"{UrlUtilities.BASE_URL}/t/{topicId}";
            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, topicUrl);
            response = await this.HttpClient.SendAsync(redirectRequest);
        }
        else
        {
            //todo: handle the reply error.
            throw new InvalidOperationException("Can not reply the topic.");
        }
        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }

    public async Task<TopicInfo> UnignoreTopic(string topicId, string once)
    {
        var url = $"/unignore/topic/{topicId}?once={once}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        var response = await this.HttpClient.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Found
            && response.Headers.Location != null)
        {
            var topicUrl = $"{UrlUtilities.BASE_URL}/t/{topicId}";
            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, topicUrl);
            response = await this.HttpClient.SendAsync(redirectRequest);
        }
        else
        {
            //todo: handle the reply error.
            throw new InvalidOperationException("Can not reply the topic.");
        }
        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }

    public async Task<UnitInfo> IgnoreReply(string replyId, string once)
    {
        var url = $"/ignore/reply/{replyId}?once={once}";
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<UnitInfo>(this.Logger);
        return result;
    }

    public async Task<NodeTopicInfo> IgnoreNode(string nodeId, string once)
    {
        var url = $"/settings/ignore/node/{nodeId}?once={once}";
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<NodeTopicInfo>(this.Logger);
        return result;
    }

    public async Task<NodeTopicInfo> UnignoreNode(string nodeId, string once)
    {
        var url = $"/settings/ignore/node/{nodeId}?once={once}";
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<NodeTopicInfo>(this.Logger);
        return result;
    }

    public async Task<TopicInfo> UnfavoriteTopic(string topicId, string once)
    {
        var url = $"/unfavorite/topic/{topicId}?once={once}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/t/{topicId}");

        var response = await this.HttpClient.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Found
            && response.Headers.Location != null)
        {
            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
            response = await this.HttpClient.SendAsync(redirectRequest);
        }
        else
        {
            //todo: handle the reply error.
            throw new InvalidOperationException("Can not reply the topic.");
        }

        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }

    public async Task<TopicInfo> FavoriteTopic(string topicId, string once)
    {
        var url = $"/favorite/topic/{topicId}?once={once}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/t/{topicId}");

        var response = await this.HttpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Found
            && response.Headers.Location != null)
        {
            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
            response = await this.HttpClient.SendAsync(redirectRequest);
        }
        else
        {
            //todo: handle the reply error.
            throw new InvalidOperationException("Can not reply the topic.");
        }

        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }

    public async Task<UnitInfo> UpTopic(string topicId, string once)
    {
        var url = $"/up/topic/{topicId}?once={once}";
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<UnitInfo>(this.Logger);
        return result;
    }

    public async Task<UnitInfo> DownTopic(string topicId, string once)
    {
        var url = $"/down/topic/{topicId}?once={once}";
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<UnitInfo>(this.Logger);
        return result;
     }

    public async Task<TopicInfo> ReplyTopic(string topicId, string content, string once)
    {
        var url = $"/t/{topicId}";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "content", content },
                { "once", once },
            })
        };
        var response = await this.HttpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Found
            && response.Headers.Location != request.RequestUri)
        {
            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, response.Headers.Location);
            response = await this.HttpClient.SendAsync(redirectRequest);
        }
        else
        {
            //todo: handle the reply error.
            throw new InvalidOperationException("Can not reply the topic.");
        }

        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }

    public async Task<UnitInfo> BlockUser(string url)
    {
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<UnitInfo>(this.Logger);
        return result;
    }

    public async Task<MemberPageInfo> FollowUser(string url)
    {
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<MemberPageInfo>(this.Logger);
        return result;
    }

    public async Task<UnitInfo> FavoriteNode(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/mission/daily");
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<UnitInfo>(this.Logger);
        return result;
    }

    public async Task<DailyInfo> GetDailyInfo()
    {
        var url = "/mission/daily";
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<DailyInfo>(this.Logger);
        return result;
    }

    public async Task<DailyInfo> CheckIn(string once)
    {
        var url = $"/mission/daily/redeem?once={once}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/mission/daily");
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<DailyInfo>(this.Logger);
        return result;
    }

    public async Task<NewsInfo> SignInTwoStep(string code, string once)
    {
        var url = "/2fa?next=/mission/daily";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", code },
                { "once", once },
            })
        };
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/mission/daily");
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<NewsInfo>(this.Logger);
        return result;
    }

    public async Task<DailyInfo> RequestByUrl(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Referer", $"{UrlUtilities.BASE_URL}/mission/daily");
        var response = await this.HttpClient.SendAsync(request);
        var result = await response.GetEncapsulatedData<DailyInfo>(this.Logger);
        return result;
    }

    public async Task<TopicInfo> FadeTopic(string url)
    {
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }

    public async Task<TopicInfo> StickyTopic(string url)
    {
        var response = await this.HttpClient.GetAsync(url);
        var result = await response.GetEncapsulatedData<TopicInfo>(this.Logger);
        return result;
    }
}
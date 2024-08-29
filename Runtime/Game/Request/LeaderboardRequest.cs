using System;
using MyriaLeaderboard.Requests;
using System.Collections.Generic;
using UnityEngine;

namespace MyriaLeaderboard.MyriaLeaderboardEnums
{
    public enum EOrderBy { ASC, DESC };
    public static class EOrderByExtensions
    {
        public static string OderByToStringValue(this EOrderBy orderBy)
        {
            switch (orderBy)
            {
                case EOrderBy.ASC:
                    return "ASC";
                case EOrderBy.DESC:
                    return "DESC";
                default:
                    return string.Empty;
            }
        }
    }
    public enum ESortingField { createdAt, updatedAt, score, rank, period, leaderboardId, userId};
    public static class ESortingFieldExtensions
    {
        public static string SortingToStringValue(this ESortingField sortingField)
        {
            switch (sortingField)
            {
                case ESortingField.createdAt:
                    return "createdAt";
                case ESortingField.updatedAt:
                    return "updatedAt";
                case ESortingField.score:
                    return "score";
                case ESortingField.rank:
                    return "rank";
                case ESortingField.period:
                    return "period";
                case ESortingField.leaderboardId:
                    return "leaderboardId";
                case ESortingField.userId:
                    return "userId";
                default:
                    return "createdAt";
            }
        }
    }
}

namespace MyriaLeaderboard.Requests
{

    public class User
    {
         public string userId { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public string username { get; set; }
        public string displayName { get; set; }
    }
    public class GetListScoreItemResponse
    {
        public int id { get; set; }
        public string userId { get; set; }
        public int leaderboardId { get; set; }
        public int score { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public int rank { get; set; }
    }
    public class GetScoreDataResponse
    {
        // we are doing thisfor legacy reasons, since it is no longer being set on the backend
        public PaginationMetaResponse meta { get; set; }
        public GetListScoreItemResponse[] items { get; set; }
    }
    public class MyriaLeaderboardGetScoreListResponse : MyriaLeaderboardResponse
    {
        // we are doing thisfor legacy reasons, since it is no longer being set on the backend
        public string status { get; set; }
        public GetScoreDataResponse data { get; set; }
    }

    // Postscore
    public class PostScoreDataResponse
    {
        // we are doing thisfor legacy reasons, since it is no longer being set on the backend
        public int id { get; set; }
        public string userId { get; set; }
        public int leaderboardId { get; set; }
        public int score { get; set; }
        public int period { get; set; }
        public string createdAt { get; set; }
        public string updatedAt { get; set; }
        public User user { get; set; }
    }

    public class MyriaLeaderboardPostScoreResponse : MyriaLeaderboardResponse
    {
        public string status { get; set; }
        public PostScoreDataResponse[] data { get; set; }
    }

    public class Leaderboard
    {
        public int leaderboard_id { get; set; }
    }
    public class MyriaLeaderboardGetScoreListRequest : MyriaLeaderboardGetRequests
    {
        public string leaderboardKey { get; set; }
        public static int? nextCursor { get; set; }
        public static int? prevCursor { get; set; }
    }
    public class MyriaLeaderboardPostScoreRequest : MyriaLeaderboardGetRequests
    {
        public string leaderboardKey { get; set; }
    }

    public class MyriaLeaderboardGetRequests
    {
        public int limit { get; set; }
        public int page { get; set; }
        public string sortingField { get; set; }
        public string orderBy { get; set; }
    }
    public class MyriaLeaderboardItemPostScore
    {
        /// <summary>
        /// Amount of the given currency to debit/credit to/from the given wallet
        /// </summary>
        public int score { get; set; }
        /// <summary>
        /// The id of the currency that the amount is given in
        /// </summary>
        public string userId { get; set; }
        /// <summary> The id of the wallet to credit/debit to/from
        /// </summary>
        public string username { get; set; }
        /// <summary> The id of the wallet to credit/debit to/from
        /// </summary>
        public string displayName { get; set; }
    }

    public class MyriaLeaderboardPostScoreParams
    {
        public MyriaLeaderboardItemPostScore[] items { get; set; }
    }
}

namespace MyriaLeaderboard
{
    public partial class MyriaLeaderboardAPIManager
    {
        public static void GetScoreList(MyriaLeaderboardGetScoreListRequest getRequests, Action<MyriaLeaderboardGetScoreListResponse> onComplete)
        {
            EndPointClass requestEndPoint = MyriaLeaderboardEndPoints.MyriaEndPointGetScoreByLeaderboardId;

            string tempEndpoint = requestEndPoint.endPoint;
            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.leaderboardKey, getRequests.page, getRequests.limit, getRequests.sortingField, getRequests.orderBy);

            // Handle all paging
            if (getRequests.page == 0)
            {
                onComplete?.Invoke(MyriaLeaderboardResponseFactory.InputUnserializableError<MyriaLeaderboardGetScoreListResponse>());
                return;
            }
            tempEndpoint = requestEndPoint.endPoint + "";
            endPoint = string.Format(tempEndpoint, getRequests.leaderboardKey, getRequests.page, getRequests.limit, getRequests.sortingField, getRequests.orderBy);

            MyriaLeaderboardServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, null, (serverResponse) =>
            { MyriaLeaderboardResponse.Deserialize(onComplete, serverResponse); }, false, MyriaLeaderboardEnums.MyriaLeaderboardCallerRole.Base);
            //{
            //    MyriaLeaderboardGetScoreListResponse parseData = MyriaLeaderboardResponse.Deserialize<MyriaLeaderboardGetScoreListResponse>(serverResponse);
            //    onComplete?.Invoke(parseData);
            //}, false, MyriaLeaderboardEnums.MyriaLeaderboardCallerRole.Base);
        }
        public static void PostScores(MyriaLeaderboardPostScoreRequest getRequests, MyriaLeaderboardPostScoreParams data, Action<MyriaLeaderboardPostScoreResponse> onComplete)
        {
            if (data == null)
            {
                onComplete?.Invoke(MyriaLeaderboardResponseFactory.InputUnserializableError<MyriaLeaderboardPostScoreResponse>());
                return;
            }
            var json = MyriaLeaderboardJson.SerializeObject(data);
            Debug.Log("json" + json);

            EndPointClass requestEndPoint = MyriaLeaderboardEndPoints.MyriaEndPointPostScoreByLeaderboardId;
            string tempEndpoint = requestEndPoint.endPoint;
            string endPoint = string.Format(requestEndPoint.endPoint, getRequests.leaderboardKey);
            endPoint = string.Format(endPoint, getRequests.leaderboardKey);
            MyriaLeaderboardServerRequest.CallAPI(endPoint, requestEndPoint.httpMethod, json, (serverResponse) => { MyriaLeaderboardResponse.Deserialize(onComplete, serverResponse); }, false, MyriaLeaderboardEnums.MyriaLeaderboardCallerRole.Base);
        }
    }
}
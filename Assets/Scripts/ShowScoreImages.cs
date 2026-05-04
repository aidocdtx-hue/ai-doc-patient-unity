using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 점수를 화면에 표시해주는 스크립트
/// </summary>
public class ShowScoreImages : MonoBehaviour
{
    //점수숫자 영역 오브젝트
    public GameObject scoreGrid;
    //타이밍 관련 추가점수이미지
    public Image timingImage;
    //+ - 부호
    public Image mark;
    //숫자 이미지
    public Image[] numbers;

    //해당 스프라이트들
    public Sprite[] timingSprites; // 완벽해요, 잘했어요
    public Sprite[] markSprites; // + -
    public Sprite[] numbersSprites; // 0~9

    //점수 관련 사운드
    public AudioSource timingAudioSource;
    public AudioSource scoreAudioSource;
    public AudioClip[] timingSounds;

    /// <summary>
    /// 점수를 전달받아서 화면에 표시하기 위해 가공하는 부분
    /// </summary>
    /// <param name="score">점수</param>
    /// <param name="timing">0 = 완벽해요, 1 = 잘했어요, 2 = 없음</param>
    public void SetScore(int score, int timing)
    {
        //현재 0점이면 점수를 표시하지 않음
        if(score == 0) {  return; }

        //점수를 string으로 변경하여 저장
        string scoreString = score.ToString();
        //점수 글자를 하나씩 저장할 리스트
        List<string> splitedScore = new List<string>();

        //점수가 2자리 이상일 경우
        if (score > 9)
        {
            //기호는 +로 설정
            mark.sprite = markSprites[0];
            //2자리 숫자 표시를 위해 숫자를 모두 키기
            numbers[1].gameObject.SetActive(true);
            //앞글자와 뒤 글자를 분리하여 리스트에 추가
            splitedScore.Add(scoreString.Substring(0, 1));
            splitedScore.Add(scoreString.Substring(1, 1));
            //자릿수에 맞는 숫자로 이미지 교체
            numbers[0].sprite = numbersSprites[int.Parse(splitedScore[0])];
            numbers[1].sprite = numbersSprites[int.Parse(splitedScore[1])];
        }
        //점수가 1자리일 경우
        else
        {
            //0보다 작으면
            if(score < 0)
            {
                //기호를 -로 설정
                mark.sprite = markSprites[1];
                //점수의 절대값을 구해서 +로 변경
                score = Mathf.Abs(score);
            }
            else
            {
                //기호를 +로 설정
                mark.sprite = markSprites[0];
            }
            //숫자 이미지를 점수에 맞게 변경
            numbers[0].sprite = numbersSprites[score];
            //10의 자리 숫자를 꺼서 보이지않게 함
            numbers[1].gameObject.SetActive(false);
        }

        // 게임오브젝트가 꺼진 다음 호출되는 부분 예외처리
        if (gameObject.activeInHierarchy)
        {
            //최종 점수 출력
            StartCoroutine(ShowScore(timing));
        }
        

    }

    /// <summary>
    /// 점수 출력
    /// </summary>
    /// <param name="timing">타이밍 점수</param>
    /// <returns></returns>
    IEnumerator ShowScore(int timing)
    {
        switch (timing)
        {
            //완벽해요
            case 0:
                //이미지를 완벽해요로 변경
                timingImage.sprite = timingSprites[0];
                //완벽해요 사운드 설정
                timingAudioSource.clip = timingSounds[0];
                //사운드 재생
                timingAudioSource.Play();
                //완벽해요 이미지 켜기
                timingImage.gameObject.SetActive(true);
                break;
            case 1:
                //이미지를 잘했어요로 변경
                timingImage.sprite = timingSprites[1];
                //잘햇어요 사운드 설정
                timingAudioSource.clip = timingSounds[1];
                //사운드 재생
                timingAudioSource.Play();
                //잘햇어요 이미지 켜기
                timingImage.gameObject.SetActive(true);
                break;
            case 2:
                //타이밍 이미지 끄기
                timingImage.gameObject.SetActive(false);
                break;

        }
        //점수영역 켜기
        scoreGrid.gameObject.SetActive(true);
        //0.5초 대기
        yield return new WaitForSeconds(0.5f);
        //점수와 타이밍이미지 끄기
        scoreGrid.gameObject.SetActive(false);
        timingImage.gameObject.SetActive(false);
        //점수 사운드 재생
        scoreAudioSource.Play();
    }
}


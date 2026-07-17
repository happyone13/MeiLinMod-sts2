-- MeiLinMod card offer/pick rate and picked-run win rate for RitsuLib PostHog events.
-- RitsuLib stores the run outcome in event properties:
--   properties.is_victory
-- and the full run in:
--   properties.payload.applicant_payload.run_history
-- Card reward choices are under:
--   payload.applicant_payload.run_history.map_point_history[].player_stats[].card_choices[]
-- Card ids may appear as CARD.MEILINMOD-... in older run history, while localization keys use MEILINMOD_...

SELECT
    card_id,
    multiIf(
        card_id = 'MEILINMOD_LONG_ZHI_JING_SHEN', '龙之精神',
        card_id = 'MEILINMOD_LONG_ZHI_CHUAN_CHENG', '龙之传承',
        card_id = 'MEILINMOD_LONG_ZHI_BEI_FU', '龙之背负',
        card_id = 'MEILINMOD_LONG_ZHI_DAO', '龙指导',
        card_id = 'MEILINMOD_QUN_LONG_ZHI_LI', '群龙之力',
        card_id = 'MEILINMOD_STRIKE_MEILIN', '打击',
        card_id = 'MEILINMOD_DEFEND_MEILIN', '防御',
        card_id = 'MEILINMOD_ATTACK_DEFENSE_UNITY', '攻防一体',
        card_id
    ) AS card_name,
    count() AS offered_count,
    countIf(was_picked) AS picked_count,
    round(picked_count / nullIf(offered_count, 0) * 100, 1) AS pick_rate_percent,
    countIf(was_picked AND is_victory) AS picked_win_count,
    round(picked_win_count / nullIf(picked_count, 0) * 100, 1) AS picked_win_rate_percent
FROM (
    SELECT
        replaceRegexpOne(
            replaceRegexpOne(JSONExtractString(card_choice, 'card', 'id'), '^CARD\\.', ''),
            '^MEILINMOD-',
            'MEILINMOD_'
        ) AS card_id,
        JSONExtractBool(card_choice, 'was_picked') AS was_picked,
        is_victory
    FROM (
        SELECT
            arrayJoin(JSONExtractArrayRaw(ifNull(JSONExtractRaw(player_stat, 'card_choices'), '[]'))) AS card_choice,
            is_victory
        FROM (
            SELECT
                arrayJoin(JSONExtractArrayRaw(ifNull(JSONExtractRaw(map_point, 'player_stats'), '[]'))) AS player_stat,
                is_victory
            FROM (
                SELECT
                    arrayJoin(JSONExtractArrayRaw(ifNull(act, '[]'))) AS map_point,
                    is_victory
                FROM (
                    SELECT
                        arrayJoin(JSONExtractArrayRaw(ifNull(
                            JSONExtractRaw(toString(coalesce(properties.payload, '{}')), 'applicant_payload', 'run_history', 'map_point_history'),
                        '[]'))) AS act,
                        lower(toString(properties.is_victory)) IN ('true', '1') AS is_victory
                    FROM events
                    WHERE event = 'run_history.completed'
                      AND properties.applicant_id = 'MeiLinMod'
                      AND properties.category = 'RunHistory'
                      AND toString(properties.run_character_ids) LIKE '%MEILIN%'
                      AND timestamp >= now() - INTERVAL 90 DAY
                )
            )
        )
    )
)
WHERE card_id LIKE 'MEILINMOD_%'
GROUP BY card_id
ORDER BY offered_count DESC
LIMIT 100

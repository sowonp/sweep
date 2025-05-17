import pandas as pd
import random
import numpy as np
import time
import heapq
import math
import os
import glob

# ================================================
# 작업 디렉토리 설정
# ================================================
os.chdir("C:/sweep_algorithm_test/")
os.makedirs("algo_test_results", exist_ok=True)

# ================================================
# CSV 데이터 로드
# ================================================
def load_data(file_path):
    return pd.read_csv(file_path)

data = load_data('2023.csv')

# ================================================
# 환경 기반 쓰레기 및 장애물 생성 함수
# ================================================
def generate_trash_obstacles_with_environment(grid_size, env_row):
    # === 환경 데이터 읽기 ===
    wind_speed = env_row['풍속(m/s)']
    wind_direction = env_row['풍향(deg)']
    gust_speed = env_row['GUST풍속(m/s)']
    max_wave_height = env_row['최대파고(m)']
    significant_wave_height = env_row['유의파고(m)']
    mean_wave_height = env_row['평균파고(m)']
    wave_period = env_row['파주기(sec)']
    wave_direction = env_row['파향(deg)']

    # === NaN 처리 (기본값 설정) ===
    if pd.isna(wind_speed):
        wind_speed = 0
    if pd.isna(wind_direction):
        wind_direction = 0
    if pd.isna(gust_speed):
        gust_speed = 0
    if pd.isna(max_wave_height):
        max_wave_height = 0
    if pd.isna(significant_wave_height):
        significant_wave_height = 0
    if pd.isna(mean_wave_height):
        mean_wave_height = 0
    if pd.isna(wave_period):
        wave_period = 0
    if pd.isna(wave_direction):
        wave_direction = 0

    # === 쓰레기와 장애물 개수 결정 ===
    base_trash = 50
    base_obstacles = 10

    trash_count = base_trash + int(wind_speed * 5) + int(significant_wave_height * 10)
    obstacle_count = base_obstacles + int(gust_speed * 2) + int(max_wave_height * 5)

    trash_count = min(trash_count, 300)
    obstacle_count = min(obstacle_count, 50)

    # === 쓰레기 배치 (바람 방향 고려) ===
    trash_positions = []
    for _ in range(trash_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)

        offset_distance = random.randint(0, 5)
        offset_x = int(np.cos(np.radians(wind_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wind_direction)) * offset_distance)

        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))

        trash_positions.append((new_x, new_y))

    # === 장애물 배치 (파향 고려) ===
    obstacle_positions = []
    for _ in range(obstacle_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)

        offset_distance = random.randint(0, 3)
        offset_x = int(np.cos(np.radians(wave_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wave_direction)) * offset_distance)

        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))

        obstacle_positions.append((new_x, new_y))

    return trash_positions, obstacle_positions

# ================================================
# Score
# ================================================
def score_algorithm(start, trash_positions, obstacle_positions, env=None):
    grid_size = 50
    directions = [(-1,0), (1,0), (0,-1), (0,1), (-1,-1), (-1,1), (1,-1), (1,1)]
    current_pos = start
    path = []

    def heuristic(a, b):
        return np.hypot(b[0] - a[0], b[1] - a[1])

    def a_star(start, goal, obstacles):
        open_set = [(heuristic(start, goal), 0, start, [start])]
        visited = set()

        while open_set:
            _, cost, current, current_path = heapq.heappop(open_set)
            if current == goal:
                return current_path
            if current in visited:
                continue
            visited.add(current)

            for dx, dy in directions:
                nx, ny = current[0] + dx, current[1] + dy
                if 0 <= nx < grid_size and 0 <= ny < grid_size and (nx, ny) not in obstacles:
                    new_cost = cost + 1
                    heapq.heappush(open_set, (new_cost + heuristic((nx, ny), goal), new_cost, (nx, ny), current_path + [(nx, ny)]))
        return None

    while trash_positions:
        scores = []
        for dx, dy in directions:
            dir_vec = np.array([dx, dy])
            score = 0
            for tx, ty in trash_positions:
                vec = np.array([tx - current_pos[0], ty - current_pos[1]])
                dist = np.linalg.norm(vec)
                if dist == 0:
                    continue
                dot = np.dot(dir_vec, vec) / (np.linalg.norm(dir_vec) * dist)
                if dot >= 0.7:
                    score += 1 / (dist + 1e-5)
            scores.append(score)

        best_dir_idx = int(np.argmax(scores))
        best_dir = directions[best_dir_idx]

        # 해당 방향 쓰레기만 필터링
        candidates = []
        for tx, ty in trash_positions:
            vx, vy = tx - current_pos[0], ty - current_pos[1]
            dot = best_dir[0]*vx + best_dir[1]*vy
            if dot > 0:
                candidates.append((tx, ty))

        if not candidates:
            candidates = trash_positions

        next_trash = min(candidates, key=lambda p: heuristic(current_pos, p))
        route = a_star(current_pos, next_trash, set(obstacle_positions))

        if route:
            path.extend(route[1:])
            current_pos = next_trash
        current_pos = next_trash
        trash_positions.remove(next_trash)

    return path

# ================================================
# 알고리즘 성능 평가 함수
# ================================================
def evaluate_algorithm(algorithm, start, trash_positions, obstacle_positions, env=None):
    start_time = time.time()
    results = algorithm(start, trash_positions.copy(), obstacle_positions, env)
    end_time = time.time()

    collection_rate = len(set(results) & set(trash_positions)) / len(trash_positions) if trash_positions else 0
    collision_count = len([pos for pos in results if pos in obstacle_positions])
    search_distance = len(results)
    elapsed_time = end_time - start_time

    return {
        "collection_rate": collection_rate,
        "collision_count": collision_count,
        "search_distance": search_distance,
        "elapsed_time": elapsed_time
    }

# ================================================
# 결과 출력 함수
# ================================================
def print_results(results):
    for name, metrics in results.items():
        print(f"{name} Algorithm:")
        print(f"  수집률: {metrics['collection_rate']:.4f}")
        print(f"  충돌 횟수: {metrics['collision_count']}")
        print(f"  탐색 거리: {metrics['search_distance']}")
        print(f"  실행 시간: {metrics['elapsed_time']:.4f} seconds")
        print()

# ================================================
# 결과 저장 함수
# ================================================
save_folder = "algo_test_results"
os.makedirs(save_folder, exist_ok=True)

algorithm_names = ["💙Score"]

def save_partial_results(all_results, save_count):
    rows = []
    for i, result in enumerate(all_results):
        for algo in algorithm_names:
            row = {
                "Index": i,
                "Algorithm": algo,
                "CollectionRate": result[algo]["collection_rate"],
                "CollisionCount": result[algo]["collision_count"],
                "SearchDistance": result[algo]["search_distance"],
                "ElapsedTime": result[algo]["elapsed_time"]
            }
            rows.append(row)
    df = pd.DataFrame(rows)
    df.to_csv(f"{save_folder}/algorithm_test_results_{save_count}.csv", index=False, encoding='utf-8-sig')

# ================================================
# 메인 실행
# ================================================
all_results = []
batch_size = 500
save_count = 1
total = len(data)

for index, row in data.iterrows():
    trash_positions, obstacle_positions = generate_trash_obstacles_with_environment(50, row)
    start = (0, 0)

    Score_results = evaluate_algorithm(score_algorithm, start, trash_positions, obstacle_positions, env=row)

    results = {
        "💙Score": Score_results,
    }

    all_results.append(results)

    print()
    print("================================================")
    print(f"Data Index {index+1}/{total}: ({(index+1)/total*100:.2f}%)")
    print()
    print_results(results)

    if (index + 1) % batch_size == 0 or (index + 1) == total:
        save_partial_results(all_results, save_count)
        print(f"Results saved: batch {save_count}")
        save_count += 1
        all_results = []

if all_results:
    save_partial_results(all_results, save_count)
    print("\n================================================")
    print(f"✅ TEST RESULT FILE SAVED: Score_results_{save_count}.csv")
    print("================================================")
    print()

# ================================================
# 평균 결과 계산
# ================================================
print("\n================================================")
print("✅ TEST RESULT AVERAGE")
file_list = glob.glob(f"{save_folder}/algorithm_test_results_*.csv")

dfs = []
for file in file_list:
    dfs.append(pd.read_csv(file))

full_df = pd.concat(dfs, ignore_index=True)

for algo in algorithm_names:
    algo_df = full_df[full_df["Algorithm"] == algo]
    print(f"\n=== {algo} Algorithm ===")
    print(f"  수집률 평균: {algo_df['CollectionRate'].mean():.4f}")
    print(f"  충돌 횟수 평균: {algo_df['CollisionCount'].mean():.4f}")
    print(f"  탐색 거리 평균: {algo_df['SearchDistance'].mean():.4f}")
    print(f"  실행 시간 평균: {algo_df['ElapsedTime'].mean():.4f} seconds")

print()